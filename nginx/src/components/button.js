/* Copyright (C) 2020-2025 b1f6c1c4
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

export default class Button {
    constructor(x, y, w, h) {
        this.xlb = x;
        this.ylb = y;
        this.xub = x + w;
        this.yub = y + h;
    }
    check(p) {
        if (p.mouseX < this.xlb) return false;
        if (p.mouseX > this.xub) return false;
        if (p.mouseY < this.ylb) return false;
        if (p.mouseY > this.yub) return false;
        return true;
    }
    dispatch(p, store, action) {
        if (this.check(p, store)) {
            store.dispatch(action);
            return true;
        }
        return false;
    }
}
