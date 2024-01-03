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

import { render } from 'preact';
import { useErrorBoundary } from 'preact/hooks';
import { html } from 'htm/preact';
import Jtysf from './containers/jtysf/jtysf';

const App = () => {
    const [error] = useErrorBoundary();
    return html`
        <h1>jtysf</h1>
        ${error && html`<pre class="error">Error: ${''+error}</pre>`}
        <${Jtysf} />
    `;
};

render(html`
    <${App} />
`, document.getElementById('app'));
