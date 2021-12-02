import p5 from 'p5';
import { store } from './app/store.js';
import App from './app/app.js';

new p5((p) => {
    new App(p, store);
}, document.getElementById('app'));
