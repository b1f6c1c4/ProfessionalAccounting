/* Copyright (C) 2022-2024 b1f6c1c4
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
import { html } from 'htm/preact';

export default function DateSelector(props) {
    const {
        title,
        onValue,
        value,
    } = props;
    const d = dayjs(value, 'YYYYMMDD');
    let date = value;
    switch (d.day()) {
        case 0: date += '[S]'; break;
        case 1: date += '[M]'; break;
        case 2: date += '[T]'; break;
        case 3: date += '[W]'; break;
        case 4: date += '[R]'; break;
        case 5: date += '[F]'; break;
        case 6: date += '[A]'; break;
    }
    const dt = dayjs().format('YYYYMMDD');
    const dp = d.subtract(1, 'day').format('YYYYMMDD');
    const dn = d.add(1, 'day').format('YYYYMMDD');
    return html`
        <p class="selector">
            <span>${title}</span>
            <span class="button" onclick=${() => onValue(dp)}>-</span>
            <span class="chosen" onclick=${() => onValue(dt)}>${date}</span>
            <span class="button" onclick=${() => onValue(dn)}>+</span>
        </p>
    `;
}
