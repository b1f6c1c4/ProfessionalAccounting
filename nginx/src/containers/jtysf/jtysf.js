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
import * as Api from '../../app/api.js';
import { useEffect, useState } from 'preact/hooks';
import { html } from 'htm/preact';
import DateSelector from '../../components/dateSelector';
import FlatSelector from '../../components/flatSelector';

const theUser = window.localStorage.getItem('user') || 'anonymous';

export default function Jtysf() {
    const [date, setDate] = useState(dayjs().format('YYYYMMDD'));
    const [company, setCompany] = useState(null);
    const [payee, setPayee] = useState(null);
    const [fund, setFund] = useState(null);
    const [payer, setPayer] = useState(null);
    const [card, setCard] = useState(null);
    const [isLoading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [response, setResponse] = useState(null);
    const [isFinished, setFinished] = useState(false);

    const form = [];
    form.push(html`<${DateSelector} title="date" value=${date} onValue=${setDate}
        isDisabled=${isLoading || isFinished} />`);
    form.push(html`<${FlatSelector} title="company" value=${company} onValue=${setCompany}
        isDisabled=${isLoading || isFinished} query="rps U T660208 G !c" />`);
    form.push(html`<${FlatSelector} title="payee" value=${payee} onValue=${setPayee}
        isDisabled=${isLoading || isFinished} query="rps U T660208 G !U" />`);

    if (payee && company) {
        const cf = `U T660208 '${company.replace(/'/g, '\'\'')}'`;
        form.push(html`<${FlatSelector} title="fund" value=${fund} onValue=${setFund}
            isDisabled=${isLoading || isFinished} query=${`rps ${cf} G !V`} />`);

        if (fund) {
            form.push(html`<${FlatSelector} title="payer" value=${payer} onValue=${setPayer}
                isDisabled=${isLoading || isFinished} query=${`rps ${cf} G : U T101201 !U`} />`);

            if (payer) {
                form.push(html`<${FlatSelector} title="card" value=${card} onValue=${setCard}
                    isDisabled=${isLoading || isFinished} query=${`rps ${cf} G : ${payer} T101201 !c`} />`);

                if (card) {
                    const txt = `new Voucher {
${date}
${payer} T101201 '${card.replace(/'/g, '\'\'')}' -${fund}
${payee} T660208 '${company.replace(/'/g, '\'\'')}' ${fund}
}`;
                    const submit = async () => {
                        setLoading(true);
                        setError(null);
                        try {
                            const { data } = await Api.voucherUpsertApi(txt, theUser);
                            setResponse(data);
                            setLoading(false);
                            setFinished(true);
                        } catch (e) {
                            setLoading(false);
                            setError(e);
                        }
                    };
                    const reset = () => {
                        setPayee(null);
                        setFund(null);
                        setPayer(null);
                        setCard(null);
                        setError(null);
                        setResponse(null);
                        setFinished(false);
                    };
                    const revoke = async () => {
                        setLoading(true);
                        setError(null);
                        try {
                            await Api.voucherRemovalApi(response.trim('@'), theUser);
                            setLoading(false);
                            setResponse(null);
                            setFinished(false);
                        } catch (e) {
                            setLoading(false);
                            setError(e);
                        }
                    };

                    const action = [];
                    action.push(html`<input type="submit" disabled=${isLoading || isFinished} onclick=${submit}
                        value=${isFinished ? 'Submitted' : 'Submit'} />`);
                    if (isFinished) {
                        action.push(html`<input type="reset" onclick=${reset} value="Another" />`);
                        action.push(html`<input type="reset" onclick=${revoke} value="Revert" class="revoke" />`);
                    }

                    form.push(html`<p class="actions">${action}</p>`);
                    if (error)
                        form.push(html`<pre class="error">Error: ${''+error}</pre>`);
                    if (response)
                        form.push(html`<pre>${response}</pre>`);
                }
            }
        }
    }

    return form;
};
