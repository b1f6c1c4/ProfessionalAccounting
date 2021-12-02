import dayjs from 'dayjs';
import Button from '../../components/button.js';
import Selector from '../../components/selector.js';
import {
    dateInc,
    dateDec,
    addPayee,
    removePayee,
    addPayer,
    removePayer,
} from './editSlice.js';

const querier = (t) => {
    const res = [];
    if (!t) {
        res.push('U!U');
        res.push('U @@ T122100 + U @@ T224100 + U @@ T630103 + U @@ T671109 -9~ !c');
    } else {
        const rt = t.replace(/'/g, '\'\'');
        res.push(`(U @@ T122100 + U @@ T224100 + U @@ T630103 + U @@ T671109) * (U '${rt}'.*) !c`);
    }
    return res;
};

export default function Edit(p, store) {
    this.payerSelector = new Selector(p, store,
        (state) => Object.keys(state.edit.editor.payers), addPayer, removePayer,
        querier, () => store.getState().edit.payees);
    this.payeeSelector = new Selector(p, store,
        (state) => Object.keys(state.edit.editor.payees), addPayee, removePayee,
        querier, () => store.getState().edit.payers);
    this.draw = function() {
        const state = store.getState().edit;
        const gap = 5;

        let editorWidth, editorHeight;
        let liveViewX, liveViewY;
        let liveViewWidth, liveViewHeight;

        p.push();

        p.fill(0);
        p.rectMode(p.CORNER);

        p.push();
        { // @media
            p.strokeWeight(2);
            p.stroke(0);
            if (p.width > p.height) {
                editorWidth = p.width * 0.6 - gap / 2;
                liveViewX = editorWidth + gap;
                liveViewWidth = p.width * 0.4 - gap / 2;
                editorHeight = p.height;
                liveViewHeight = p.height;
                liveViewY = 0;
                p.line(liveViewX - gap / 2, liveViewY, liveViewX - gap / 2, editorHeight);
            } else {
                editorWidth = p.width;
                liveViewWidth = p.width;
                liveViewX = 0;
                editorHeight = p.height * 0.6 - gap / 2;
                liveViewHeight = p.height * 0.4 - gap / 2;
                liveViewY = editorHeight + gap;
                p.line(liveViewX, liveViewY - gap / 2, editorWidth, liveViewY - gap / 2);
            }
        }
        p.pop();

        let editorRows = 5
            + state.editor.details.length
            + state.editor.payments.length;
        if (editorRows < 8)
            editorRows = 8;
        const editorRowHeight = editorHeight / editorRows;
        let row = 0;
        p.push();
        { // editor line 1: date, payer, payee
            p.translate(0, row * editorRowHeight);
            p.push();
            p.stroke(170);
            p.line(0, editorRowHeight, editorWidth, editorRowHeight);
            p.pop();

            p.textSize(editorRowHeight * 0.5);
            let baseLine = editorRowHeight * 0.62;
            let date = state.editor.date;
            switch (dayjs(date, 'YYYYMMDD').day()) {
                case 0: date += '[S]'; break;
                case 1: date += '[M]'; break;
                case 2: date += '[T]'; break;
                case 3: date += '[W]'; break;
                case 4: date += '[R]'; break;
                case 5: date += '[F]'; break;
                case 6: date += '[A]'; break;
            }
            p.textAlign(p.LEFT);
            p.text(date, gap, baseLine);
            const dateWidth = p.textWidth(date);

            let arrowWidth = 25;
            p.textSize(editorRowHeight * 0.3);
            baseLine = editorRowHeight * 0.58;
            const payers = state.editor.payers;
            let payerList = Object.keys(payers).join('&');
            if (!payerList)
                payerList = 'anonymous';
            const payerWidth = p.textWidth(payerList);
            const payees = state.editor.payees;
            let payeeList = Object.keys(payees).join('&');
            if (!payeeList)
                payeeList = 'anonymous';
            const payeeWidth = p.textWidth(payeeList);
            p.textAlign(p.RIGHT);
            p.text(payerList, editorWidth - gap - payeeWidth - 2 * gap - arrowWidth, baseLine);
            p.push();
            { // arrow
                p.translate(editorWidth - gap - payeeWidth - gap - arrowWidth, editorRowHeight / 2);
                p.strokeWeight(4);
                p.stroke(0);
                p.line(0, 0, arrowWidth, 0);
                p.line(arrowWidth, 0, arrowWidth * 0.7, +arrowWidth * 0.3);
                p.line(arrowWidth, 0, arrowWidth * 0.7, -arrowWidth * 0.3);
            }
            p.pop();
            p.text(payeeList, editorWidth - gap, baseLine);

            this.btnDateDec = new Button(0, row * editorRowHeight, dateWidth / 2, editorRowHeight);
            this.btnDateInc = new Button(dateWidth / 2, row * editorRowHeight, dateWidth, editorRowHeight);
            this.btnPayers = new Button(editorWidth - gap - payeeWidth - 2 * gap - arrowWidth - payerWidth, row * editorRowHeight, payerWidth, editorRowHeight);
            this.btnPayees = new Button(editorWidth - gap - payeeWidth, row * editorRowHeight, payeeWidth, editorRowHeight);
            row++;
        }
        p.pop();

        p.push();
        { // liveView
            let liveViewText = state.liveViewText;
            if (!liveViewText)
                liveViewText = '(no live view available)';
            p.textAlign(p.LEFT);
            p.textSize(16);
            p.textWrap(p.CHAR);
            p.text(liveViewText, liveViewX, liveViewY, liveViewWidth, liveViewHeight);
        }
        p.pop();

        p.pop();

        this.payerSelector.draw();
        this.payeeSelector.draw();
    };
    this.mouseClicked = function() {
        if (!this.payerSelector.mouseClicked()) return;
        if (!this.payeeSelector.mouseClicked()) return;
        this.btnDateDec.dispatch(p, store, dateDec());
        this.btnDateInc.dispatch(p, store, dateInc());
        if (this.btnPayers.check(p))
            this.payerSelector.activate();
        if (this.btnPayees.check(p))
            this.payeeSelector.activate();
    }
}
