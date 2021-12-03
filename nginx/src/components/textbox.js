export default class Textbox {
    constructor(p, store, selector, modifier) {
        this.p = p;
        this.store = store;
        this.selector = selector;
        this.modifier = modifier;
        this.input = null;
        this.active = false;
        this.debounce = 0;
    }

    activate() {
        this.active = true;
        this.input = this.p.createInput(this.selector(this.store.getState()));
        this.input.position(this.p.width * 0.15, this.p.height * 0.45);
        this.input.size(this.p.width * 0.7, this.p.height * 0.1);
        this.input.elt.focus();
        this.input.elt.select();
        this.p.redraw();
        this.debounce = +new Date();
    }

    deactivate() {
        this.active = false;
        this.input.remove();
        this.autocomplete = null;
        this.p.redraw();
    }

    draw() {
        if (!this.active) return;
        this.p.background(0, 127); // darken
    }

    mouseClicked() {
        if (!this.active) return true;
        if (new Date() - this.debounce < 150) return false;
        const p = this.p;
        if (p.mouseX < p.width * 0.15 || p.mouseX > p.width * 0.85 ||
            p.mouseY < p.height * 0.45 || p.mouseY > p.height * 0.55) {
            this.store.dispatch(this.modifier(this.input.value()));
            this.deactivate();
            return false;
        }
        return true;
    }
};
