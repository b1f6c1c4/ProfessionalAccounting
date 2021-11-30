import p5 from 'p5';
import { store } from './app/store.js';
import Edit from './features/edit/edit.js';

new p5((p) => {
    new Edit(p, store);
}, document.getElementById('app'));
