import p5 from 'p5';
import { store } from './app/store.js';
import App from './app/app.js';

new p5((p) => {
    p.redraw = () => {
        window.setTimeout(() => { p.draw(); }, 0);
    };
    new App(p, store);
    p.noLoop();
    store.subscribe(p.draw);
}, document.getElementById('app'));
