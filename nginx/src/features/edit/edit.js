export default function Edit(p, store) {
    p.setup = function() {
        p.createCanvas(800, 600);
    };
    p.draw = function() {
        const date = store.getState().edit.editor.date;
        p.background(0);
        p.fill(255);
        p.textSize(32);
        p.text(date, 20, 20);
    };
}
