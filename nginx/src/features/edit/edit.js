export default function Edit(p, store) {
    this.draw = function() {
        const date = store.getState().edit.editor.date;
        p.background(0);
        p.fill(255);
        p.textSize(32);
        p.text(date, 20, 20);
    };
}
