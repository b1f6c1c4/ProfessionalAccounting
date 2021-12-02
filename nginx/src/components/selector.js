import * as Api from '../app/api.js';
import _ from 'lodash';

export default class Selector {
    constructor(p, store, selector, adder, remover, query, aux) {
        this.p = p;
        this.store = store;
        this.selector = selector;
        this.remover = remover;
        this.adder = adder;
        this.remover = remover;
        this.query = query;
        this.aux = aux;
        this.autocomplete = null;
        this.candidates = null;
        this.input = null;
        this.error = null;
        this.active = false;
        this.debounce = 0;
        this.rowHeight = NaN;
        this.selected = [];
    }

    activate() {
        this.active = true;
        this.input = this.p.createInput('');
        this.input.input(this.doFetch.bind(this));
        this.input.position(this.p.width * 0.15, this.p.height * 0.15);
        this.input.size(this.p.width * 0.7, this.p.height * 0.05);
        this.p.redraw();
        this.debounce = +new Date();
    }

    deactivate() {
        this.active = false;
        this.input.remove();
        this.autocomplete = null;
        this.p.redraw();
    }

    async doFetch() {
        const tmp = [];
        if (this.aux) {
            const a = this.aux();
            if (a)
                tmp.push(...a);
        }
        try {
            const queries = this.query(this.input.value());
            for (const q of queries) {
                const { data } = await Api.safeApi('rps ' + q);
                tmp.push(...data.split('\n').filter((s) => s).map((s) => s.split('\t')[0]));
            }
            this.autocomplete = _.uniq(tmp);
            this.error = null;
        } catch (e) {
            console.error(e);
            this.autocomplete = [];
            this.error = e.message;
        }
        this.p.redraw();
    }

    draw() {
        if (!this.active) return;

        this.selected = this.selector(this.store.getState());
        if (!this.autocomplete) {
            this.doFetch();
            this.candidates = null;
        } else {
            this.candidates = _.without(this.autocomplete, ...this.selected);
        }

        const p = this.p;
        const offsetX = p.width * 0.15;
        const width = p.width * 0.7;
        const offsetY = p.height * 0.2;
        const height = p.height * 0.7;

        p.push();

        p.background(0, 127); // darken
        p.translate(offsetX, offsetY);
        p.fill(70);
        p.rect(0, 0, width, height);

        let row = 0;
        p.push();
        { // selected
            let rows = this.selected.length;
            if (this.candidates)
                rows += this.candidates.length;
            this.rowHeight = height / rows;
            p.textAlign(p.RIGHT);
            p.textSize(15);
            for (const s of this.selected) {
                p.noStroke();
                p.fill(0, 70, 0);
                p.rect(0, row * this.rowHeight, width, this.rowHeight);
                p.fill(250);
                p.text(s, width, (row + 0.5) * this.rowHeight);
                p.strokeWeight(1);
                p.stroke(200);
                p.line(0, (row + 1) * this.rowHeight, width, (row + 1) * this.rowHeight);
                row++;
            }
        }
        p.pop();

        p.push();
        { // candidates
            if (this.error) {
                p.textAlign(p.LEFT);
                p.textWrap(p.WORD);
                p.textSize(32);
                p.fill(255, 0, 0);
                p.text('Error:' + this.error, 0, 0, width, height);
            } else if (this.candidates) {
                p.textAlign(p.LEFT);
                p.textSize(13);
                p.fill(250);
                for (const a of this.candidates) {
                    p.noStroke();
                    p.text(a, 0, (row + 0.5) * this.rowHeight);
                    p.strokeWeight(1);
                    p.stroke(200);
                    p.line(0, (row + 1) * this.rowHeight, width, (row + 1) * this.rowHeight);
                    row++;
                }
            } else {
                p.textAlign(p.CENTER);
                p.textSize(32);
                p.fill(150);
                p.text('Loading', width / 2, height / 2);
            }
        }
        p.pop();

        p.pop();
    }

    mouseClicked() {
        if (!this.active) return true;
        if (new Date() - this.debounce < 150) return false;
        const p = this.p;
        if (p.mouseX < p.width * 0.15 || p.mouseX > p.width * 0.85 ||
            p.mouseY < p.height * 0.15 || p.mouseY > p.height * 0.85) {
            this.deactivate();
            return false;
        }
        const row = Math.floor((p.mouseY - p.height * 0.20) / this.rowHeight);
        if (row < this.selected.length) {
            this.store.dispatch(this.remover(this.selected[row]));
        } else if (this.candidates) {
            this.store.dispatch(this.adder(this.candidates[row - this.selected.length]));
        }
        return false;
    }
};
