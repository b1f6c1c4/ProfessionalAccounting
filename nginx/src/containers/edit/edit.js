/* Copyright (C) 2020-2023 b1f6c1c4
 *
 * This file is part of ProfessionalAccounting.
 *
 * ProfessionalAccounting is free software: you can redistribute it and/or
 * modify it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, version 3.
 *
 * ProfessionalAccounting is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Affero General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with ProfessionalAccounting.  If not, see
 * <https://www.gnu.org/licenses/>.
 */

import dayjs from 'dayjs';
import Button from '../../components/button.js';
import Selector from '../../components/selector.js';
import Textbox from '../../components/textbox.js';
import {
    dateInc,
    dateDec,
    addPayee,
    removePayee,
    updatePayer,
    updateTitle,
    updateSubtitle,
    updateContent,
    updateFund,
    removeDetail,
    newDetail,
    updateT,
    updateD,
    submitVoucherRequested,
    revertVoucherRequested,
    resetForm,
    clearError,
} from './editSlice.js';

const personQuerier = (t) => {
    const res = [];
    if (!t) {
        res.push('U!U');
        res.push('U T122100 + U T224100 + U T630103 + U T671109 -9~ !Uc');
    } else {
        const rt = t.replace(/'/g, '\'\'');
        res.push(`(U T122100 + U T224100 + U T630103 + U T671109) * (U '${rt}'.*) !Uc`);
    }
    return res;
};

export default function Edit(p, store) {
    this.activeDetailId = null;
    this.activeDetail = function() {
        if (this.activeDetailId === -1)
            return store.getState().edit.editor.payment;
        return store.getState().edit.editor.details[this.activeDetailId];
    };
    this.activeUser = function() {
        if (this.activeDetailId === -1)
            return store.getState().edit.editor.payer;
        return 'U';
    };
    this.payerSelector = new Selector(p, store, {
        selector: (state) => {
            const { payer } = state.edit.editor;
            if (!payer) return [];
            return [payer];
        },
        adder: updatePayer,
        remover: () => updatePayer(''),
        query: personQuerier,
        single: true,
    });
    this.payeeSelector = new Selector(p, store, {
        selector: (state) => Object.keys(state.edit.editor.payees),
        adder: addPayee,
        remover: removePayee,
        query: personQuerier,
        aux: () => {
            const { payer } = store.getState().edit.editor;
            if (!payer) return [];
            return [payer];
        },
    });
    this.titleSelector = new Selector(p, store, {
        selector: (state) => {
            const { title } = this.activeDetail();
            if (!title) return [];
            return [title];
        },
        adder: (t) => updateTitle({ id: this.activeDetailId, title: t }),
        remover: (t) => updateTitle({ id: this.activeDetailId, title: '' }),
        query: (t) => {
            if (this.activeDetailId !== -1) {
                return [`U Expense -9~ !t`];
            }
            return [`${this.activeUser()} * (U T1001 + U T1002 + U T1012 + U T2001 + U T2201 + U T2241) -9~ !t`];
        },
        trim: (s) => {
            if (s.startsWith('T')) return s.substr(1);
            return s;
        },
        single: true,
    });
    this.subtitleSelector = new Selector(p, store, {
        selector: (state) => {
            const { subtitle } = this.activeDetail();
            if (!subtitle) return [];
            return [subtitle];
        },
        adder: (t) => updateSubtitle({ id: this.activeDetailId, subtitle: t }),
        remover: (t) => updateSubtitle({ id: this.activeDetailId, subtitle: '' }),
        query: (t) => {
            const { title } = this.activeDetail();
            return [`${this.activeUser()} T${title.split(':')[0]} -9~ !ts`];
        },
        trim: (s) => {
            const sp = s.split('-')[1];
            if (sp && sp.startsWith(':')) return `00${sp}`;
            return sp;
        },
        single: true,
    });
    this.contentSelector = new Selector(p, store, {
        selector: (state) => {
            const { content } = this.activeDetail();
            if (!content) return [];
            return [content];
        },
        adder: (t) => updateContent({ id: this.activeDetailId, content: t }),
        remover: (t) => updateContent({ id: this.activeDetailId, content: 0 }),
        query: (t) => {
            const { title, subtitle } = this.activeDetail();
            const s = `${this.activeUser()} T${title.split(':')[0]}${subtitle.split(':')[0]}`;
            if (t) return [`${s} '${t.replace(/'/g, '\'\'')}'.* !c`];
            return [`${s} -9~ !c`];
        },
        single: true,
    });
    this.textboxFund = new Textbox(p, store,
        (state) => this.activeDetail().fund,
        (v) => updateFund({ id: this.activeDetailId, fund: v }));
    this.textboxT = new Textbox(p, store,
        (state) => state.edit.editor.adjustments.t,
        updateT);
    this.textboxD = new Textbox(p, store,
        (state) => state.edit.editor.adjustments.d,
        updateD);

    this.draw = function() {
        const state = store.getState().edit;
        const gap = 5;

        let editorWidth, editorHeight;
        let liveViewX, liveViewY;
        let liveViewWidth, liveViewHeight;

        p.push();

        p.fill(0);
        p.rectMode(p.CORNER);
        p.textFont('monospace');

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

        let editorRows = 3 + state.editor.details.length;
        const minimumRows = Math.round(editorHeight / (editorWidth / 10));
        if (editorRows < minimumRows)
            editorRows = minimumRows;
        const editorRowHeight = editorHeight / editorRows;
        let row = 0;
        p.push();
        { // editor line 1: date, payer, payees
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

            let arrowWidth = editorRowHeight * 0.4;
            p.textSize(editorRowHeight * 0.3);
            baseLine = editorRowHeight * 0.58;
            let payer = state.editor.payer;
            if (!payer)
                payer = 'anonymous';
            const payerWidth = p.textWidth(payer);
            const payees = state.editor.payees;
            let payeeList = Object.keys(payees).join('+');
            if (!payeeList)
                payeeList = 'anonymous';
            const payeeWidth = p.textWidth(payeeList);
            p.textAlign(p.RIGHT);
            p.text(payer, editorWidth - gap - payeeWidth - 2 * gap - arrowWidth, baseLine);
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
            this.btnDateInc = new Button(dateWidth / 2, row * editorRowHeight, dateWidth / 2, editorRowHeight);
            this.btnPayer = new Button(editorWidth - gap - payeeWidth - 2 * gap - arrowWidth - payerWidth, row * editorRowHeight, payerWidth, editorRowHeight);
            this.btnPayees = new Button(editorWidth - gap - payeeWidth, row * editorRowHeight, payeeWidth, editorRowHeight);
            row++;
        }
        p.pop();

        p.push();
        this.btnDetails = [];
        { // editor line 2: details
            for (const d of state.editor.details) {
                p.push();
                p.translate(0, row * editorRowHeight);
                p.push();
                p.stroke(170);
                p.line(0, editorRowHeight, editorWidth, editorRowHeight);
                p.pop();

                const btnSize = editorRowHeight * 0.5;
                const btnOffset = editorRowHeight * 0.25;
                p.push();
                p.fill(90);
                p.rect(btnOffset, btnOffset, btnSize, btnSize);
                p.textSize(editorRowHeight * 0.3);
                let baseLine = editorRowHeight * 0.58;
                p.textAlign(p.CENTER);
                p.fill(255, 230, 230);
                p.text('x', btnOffset + btnSize * 0.5, baseLine);
                p.pop();

                p.textSize(editorRowHeight * 0.5);
                baseLine = editorRowHeight * 0.68;
                const title = d.title ? 'T' + d.title.split(':')[0] : 'T????';
                const titleWidth = p.textWidth(title);
                p.text(title, btnSize + 2 * btnOffset, baseLine);

                const subtitle = d.subtitle ? d.subtitle.split(':')[0] : '00';
                const subtitleWidth = p.textWidth(subtitle);
                p.text(subtitle, btnSize + 2 * btnOffset + gap + titleWidth, baseLine);

                let content = d.content;
                if (!d.content)
                    content = '\'\'';
                let contentWidth = p.textWidth(content);
                if (btnSize + 2 * btnOffset + 4 * gap + titleWidth + subtitleWidth + contentWidth > editorWidth * 0.5) {
                    content = `${content.substr(0, 2)}...`;
                    contentWidth = p.textWidth(content);
                }
                p.text(content, btnSize + 2 * btnOffset + 4 * gap + titleWidth + subtitleWidth, baseLine);
                const sepX = btnSize + 2 * btnOffset + 10 * gap + titleWidth + subtitleWidth + contentWidth;

                p.push();
                p.strokeWeight(5);
                p.stroke(175);
                p.line(sepX, gap, sepX, editorRowHeight - gap);
                p.pop();

                let fund = d.fund;
                let fundWidth = p.textWidth(fund);
                if (sepX + fundWidth > editorWidth - gap) {
                    fund = `${fund.substr(0, 2)}...`;
                    fundWidth = p.textWidth(fund);
                }
                p.textAlign(p.RIGHT);
                p.text(fund, editorWidth - gap, baseLine);

                this.btnDetails.push({
                    btnRemove: new Button(btnOffset, row * editorRowHeight + btnOffset, btnSize, btnSize),
                    btnTitle: new Button(btnSize + 2 * btnOffset, row * editorRowHeight, titleWidth, editorRowHeight),
                    btnSubtitle: new Button(btnSize + 2 * btnOffset + gap + titleWidth, row * editorRowHeight, subtitleWidth, editorRowHeight),
                    btnContent: new Button(btnSize + 2 * btnOffset + 2 * gap + titleWidth + gap + subtitleWidth, row * editorRowHeight, contentWidth, editorRowHeight),
                    btnFund: new Button(sepX, row * editorRowHeight, editorWidth - gap - sepX, editorRowHeight),
                });
                p.pop();
                row++;
            }
        }
        p.pop();

        p.push();
        { // editor line 3: append, t, d
            p.translate(0, row * editorRowHeight);
            p.push();
            p.stroke(170);
            p.line(0, editorRowHeight, editorWidth, editorRowHeight);
            p.pop();

            const btnSize = editorRowHeight * 0.5;
            const btnOffset = editorRowHeight * 0.25;
            p.push();
            p.fill(90);
            p.rect(btnOffset, btnOffset, btnSize, btnSize);
            p.textSize(editorRowHeight * 0.3);
            let baseLine = editorRowHeight * 0.58;
            p.textAlign(p.CENTER);
            p.fill(230, 255, 230);
            p.text('+', btnOffset + btnSize * 0.5, baseLine);
            p.pop();

            p.textSize(editorRowHeight * 0.5);
            baseLine = editorRowHeight * 0.68;
            const t = 't' + state.editor.adjustments.t;
            const tWidth = p.textWidth(t);
            p.text(t, 2 * btnOffset + btnSize, baseLine);
            const d = 'd' + state.editor.adjustments.d;
            const dWidth = p.textWidth(d);
            p.text(d, 3 * btnOffset + btnSize + tWidth, baseLine);

            this.btnAppend = new Button(btnOffset, row * editorRowHeight + btnOffset, btnSize, btnSize);
            this.btnT = new Button(2 * btnOffset + btnSize, row * editorRowHeight, tWidth, editorRowHeight);
            this.btnD = new Button(3 * btnOffset + btnSize + tWidth, row * editorRowHeight, tWidth, editorRowHeight);
            row++;
        }
        p.pop();

        p.push();
        row = editorRows - 1;
        { // editor line 4: payment, checksum
            const d = state.editor.payment;
            p.translate(0, row * editorRowHeight);
            p.push();
            p.stroke(70);
            p.line(0, 0, editorWidth, 0);
            p.pop();

            p.textSize(editorRowHeight * 0.5);
            let baseLine = editorRowHeight * 0.68;
            if (d) {
                const title = d.title ? 'T' + d.title.split(':')[0] : 'T????';
                const titleWidth = p.textWidth(title);
                p.text(title, gap, baseLine);

                const subtitle = d.subtitle ? d.subtitle.split(':')[0] : '00';
                const subtitleWidth = p.textWidth(subtitle);
                p.text(subtitle, 2 * gap + titleWidth, baseLine);

                let content = d.content;
                if (!d.content)
                    content = '\'\'';
                let contentWidth = p.textWidth(content);
                if (4 * gap + titleWidth + subtitleWidth + contentWidth > editorWidth * 0.5) {
                    content = `${content.substr(0, 2)}...`;
                    contentWidth = p.textWidth(content);
                }
                p.text(content, 4 * gap + titleWidth + subtitleWidth, baseLine);
                const sepX = 10 * gap + titleWidth + subtitleWidth + contentWidth;

                p.push();
                p.strokeWeight(5);
                p.stroke(175);
                p.line(sepX, gap, sepX, editorRowHeight - gap);
                p.pop();

                this.btnPayment = {
                    btnTitle: new Button(gap, row * editorRowHeight, titleWidth, editorRowHeight),
                    btnSubtitle: new Button(2 * gap + titleWidth, row * editorRowHeight, subtitleWidth, editorRowHeight),
                    btnContent: new Button(4 * gap + titleWidth + gap + subtitleWidth, row * editorRowHeight, contentWidth, editorRowHeight),
                };
            } else {
                this.btnPayment = null;
            }

            p.fill(127, 0, 0);
            const fund = `p${state.editor.checksum.payment} d${state.editor.checksum.discount}`;
            p.textAlign(p.RIGHT);
            p.text(fund, editorWidth - gap, baseLine);
            row++;
        }
        p.pop();

        p.push();
        { // liveView
            let liveViewText = state.liveViewText;
            if (!liveViewText)
                liveViewText = '(no live view available)';
            p.textLeading(16);
            p.textAlign(p.LEFT);
            p.textSize(16);
            p.text(liveViewText, liveViewX, liveViewY, liveViewWidth, liveViewHeight);
        }
        p.pop();

        p.push();
        if (state.liveViewText) { // submit
            p.noStroke();
            if (state.committed) {
                p.fill(100, 0, 0);
            } else {
                p.fill(0, 100, 0);
            }
            p.circle(liveViewX + 20 + 30, liveViewY + liveViewHeight - 20 - 30, 60);
            this.btnSubmitRevert = new Button(liveViewX + 20, liveViewY + liveViewHeight - 20 - 60, 60, 60);
            let txt;
            if (state.committed) {
                txt = 'R';
            } else {
                txt = 'S';
            }
            p.fill(255);
            p.textSize(30);
            p.textAlign(p.CENTER);
            p.text(txt, liveViewX + 20 + 30, liveViewY + liveViewHeight - 20 - 30 + 10);
        }
        p.pop();

        p.push();
        if (state.committed) { // reset
            p.noStroke();
            p.fill(0, 0, 100);
            p.circle(liveViewX + 20 + 30 + 20 + 60, liveViewY + liveViewHeight - 20 - 30, 60);
            this.btnReset = new Button(liveViewX + 20 + 20 + 60, liveViewY + liveViewHeight - 20 - 60, 60, 60);
            p.fill(255);
            p.textSize(30);
            p.textAlign(p.CENTER);
            p.text('+', liveViewX + 20 + 30 + 20 + 60, liveViewY + liveViewHeight - 20 - 30 + 10);
        }
        p.pop();

        p.pop();

        this.payerSelector.draw();
        this.payeeSelector.draw();
        this.titleSelector.draw();
        this.subtitleSelector.draw();
        this.contentSelector.draw();
        this.textboxFund.draw();
        this.textboxT.draw();
        this.textboxD.draw();

        if (store.getState().edit.loading) {
            p.push();
            p.background(0, 127);
            p.textSize(20);
            p.textAlign(p.CENTER);
            p.fill(255);
            p.text('Loading', p.width / 2, p.height / 2);
            p.pop();
        }
        if (store.getState().edit.error) {
            p.push();
            p.background(0, 127);
            p.textSize(20);
            p.textAlign(p.CENTER);
            p.fill(200, 0, 0);
            p.text(store.getState().edit.error, p.width / 2, p.height / 2);
            p.pop();
        }
    };

    this.mouseClicked = function() {
        if (store.getState().edit.loading) return true;
        if (store.getState().edit.error) {
            store.dispatch(clearError());
            return true;
        }

        if (!this.payerSelector.mouseClicked()) return false;
        if (!this.payeeSelector.mouseClicked()) return false;
        if (!this.titleSelector.mouseClicked()) return false;
        if (!this.subtitleSelector.mouseClicked()) return false;
        if (!this.contentSelector.mouseClicked()) return false;
        if (!this.textboxFund.mouseClicked()) return false;
        if (!this.textboxT.mouseClicked()) return false;
        if (!this.textboxD.mouseClicked()) return false;

        if (this.btnDateDec.dispatch(p, store, dateDec())) return false;
        if (this.btnDateInc.dispatch(p, store, dateInc())) return false;
        if (this.btnPayer.check(p)) {
            this.payerSelector.activate();
            return false;
        }
        if (this.btnPayees.check(p)) {
            this.payeeSelector.activate();
            return false;
        }

        for (let i = -1; i < this.btnDetails.length; i++) {
            const btns = i === -1 ? this.btnPayment : this.btnDetails[i];
            if (!btns) continue;
            if (btns.btnRemove && btns.btnRemove.check(p)) {
                store.dispatch(removeDetail(i));
                return false;
            }
            if (btns.btnTitle.check(p)) {
                this.activeDetailId = i;
                this.titleSelector.activate();
                return false;
            }
            if (btns.btnSubtitle.check(p)) {
                this.activeDetailId = i;
                this.subtitleSelector.activate();
                return false;
            }
            if (btns.btnContent.check(p)) {
                this.activeDetailId = i;
                this.contentSelector.activate();
                return false;
            }
            if (btns.btnFund && btns.btnFund.check(p)) {
                this.activeDetailId = i;
                this.textboxFund.activate();
                return false;
            }
        }
        if (this.btnAppend.dispatch(p, store, newDetail())) return false;
        if (this.btnT.check(p)) {
            this.textboxT.activate();
            return false;
        };
        if (this.btnD.check(p)) {
            this.textboxD.activate();
            return false;
        };

        if (this.btnSubmitRevert && this.btnSubmitRevert.check(p, store)) {
            if (store.getState().edit.committed)
                store.dispatch(revertVoucherRequested());
            else
                store.dispatch(submitVoucherRequested());
            return false;
        }

        if (this.btnReset && this.btnReset.dispatch(p, store, resetForm())) return false;

        return true;
    }
}
