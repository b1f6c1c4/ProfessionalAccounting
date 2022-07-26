/* Copyright (C) 2022 b1f6c1c4
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
import * as Api from '../../app/api.js';
import { useEffect, useState } from 'preact/hooks';
import { html } from 'htm/preact';
import DateSelector from '../../components/dateSelector';
import FlatSelector from '../../components/flatSelector';

export default function Jtysf() {
    const [date, setDate] = useState(dayjs().format('YYYYMMDD'));
    const [payee, setPayee] = useState(null);
    const [company, setCompany] = useState(null);
    const [fund, setFund] = useState(null);
    const [payer, setPayer] = useState(null);
    const [card, setCard] = useState(null);

    const form = [];
    form.push(html`<${DateSelector} title="date" value=${date} />`);
    form.push(html`<${FlatSelector} title="payee" value=${payee} query="rps U T660208 G !U" />`);
    form.push(html`<${FlatSelector} title="company" value=${company} query="rps U T660208 G !c" />`);
    if (payee && company) {
        form.push(html`<${FlatSelector} title="fund" value=${fund} query="rps U T660208 G !U" />`);
    }
    form.push(html`<${FlatSelector} title="payer" value=${payer} query="U T101201 G !U" />`);
};
