/* Copyright (C) 2021-2025 Iori Oikawa
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
