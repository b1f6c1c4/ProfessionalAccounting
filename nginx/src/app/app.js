import Edit from '../features/edit/edit.js';

export default function App(p, store) {
    let inst;
    p.setup = function() {
        p.createCanvas(p.windowWidth, p.windowHeight);
    };
    p.draw = function() {
        if (!inst) {
            console.log('Navigating to edit');
            // TODO: dynamically decide which container to use
            inst = new Edit(p, store);
        }
        inst.draw();
    };
    p.windowResized = function() {
        p.resizeCanvas(p.windowWidth, p.windowHeight);
        if (inst.windowResized)
            inst.windowResized();
    };
}
