import dayjs from 'dayjs';
import Button from '../../components/button.js';
import { dateInc, dateDec } from './editSlice.js';

export default function Edit(p, store) {
    this.draw = function() {
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
            + store.getState().edit.editor.details.length
            + store.getState().edit.editor.payments.length;
        if (editorRows < 8)
            editorRows = 8;
        const editorRowHeight = editorHeight / editorRows;
        let row = 0;
        p.push();
        { // editor line 1: date, user
            p.translate(0, row * editorRowHeight);
            p.push();
            p.stroke(170);
            p.line(0, editorRowHeight, editorWidth, editorRowHeight);
            p.pop();

            p.textSize(editorRowHeight * 0.5);
            let baseLine = editorRowHeight * 0.62;
            let date = store.getState().edit.editor.date;
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
            const user = store.getState().edit.editor.user;
            p.textAlign(p.RIGHT);
            p.text(user, editorWidth - gap, baseLine);

            this.dateDec = new Button(dateDec, 0, row * editorRowHeight, dateWidth / 2, editorRowHeight);
            this.dateInc = new Button(dateInc, dateWidth / 2, row * editorRowHeight, dateWidth, editorRowHeight);
            row++;
        }
        p.pop();

        p.push();
        { // liveView
            let liveViewText = store.getState().edit.liveViewText;
            if (!liveViewText)
                liveViewText = '(no live view available)';
            p.textAlign(p.LEFT);
            p.textSize(16);
            p.textWrap(p.CHAR);
            p.text(liveViewText, liveViewX, liveViewY, liveViewWidth, liveViewHeight);
        }
        p.pop();

        p.pop();
    };
    this.mouseClicked = function () {
        this.dateDec.check(p, store);
        this.dateInc.check(p, store);
    }
}
