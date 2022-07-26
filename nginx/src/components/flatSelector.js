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

import * as Api from '../app/api.js';
import { useEffect, useState } from 'preact/hooks';
import { html } from 'htm/preact';

const theUser = window.localStorage.getItem('user') || 'anonymous';

export default function FlatSelector(props) {
    const {
        title,
        value,
        onValue,
    } = props;
    const [entries, setEntries] = useState(null);
    const [error, setError] = useState(null);
    useEffect(() => {
        async function go() {
            try {
                const { data } = await Api.safeApi(props.query, theUser);
                const res = data.split('\n').map((s) => s.split('\t')[0]).filter((s) => s);
                setEntries(res);
                if (res.length === 1 && !value)
                    onValue(res[0]);
                setError(null);
            } catch (e) {
                setError(e);
            }
        }
        go();
    }, [props.query, setEntries, setError]);
    return (html`
        <p class="selector">
            <span>${title}</span>
            ${error
                ? html`<span class="error">E: ${''+error}</span>`
                : value
                    ? html`<span class="chosen"
                        onclick=${() => onValue(null)}>${value}</span>`
                    : Array.isArray(entries)
                        ? entries.map((ent) => html`
                            <input type="button" value=${ent} class="button"
                                onclick=${() => onValue(ent)} />`)
                        : '...'}
        </p>
    `);
}
