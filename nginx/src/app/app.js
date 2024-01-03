/* Copyright (C) 2020-2024 b1f6c1c4
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

import Edit from '../containers/edit/edit.js';

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
        // clear everything
        p.background(200);
        inst.draw();
    };
    p.windowResized = function() {
        p.resizeCanvas(p.windowWidth, p.windowHeight);
        if (inst.windowResized)
            inst.windowResized();
    };
    p.touchEnded = function(event) {
        if (inst.mouseClicked)
            inst.mouseClicked(event);
    };
}
