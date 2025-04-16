/* Copyright (C) 2022-2025 b1f6c1c4
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

window.addEventListener('load', () => {
    const lst = document.createElement('ul');
    lst.innerHTML = `
    <li><a href="/"><span>CLI</span></a></li>
    <li><a href="/gui-discount.html"><span>!</span></a></li>
    <li><a href="/gui-jtysf.html"><span>jtysf</span></a></li>
    <li><a href="/gui-taobao.html"><span>taobao</span></a></li>
`;
    lst.classList.add('hide');
    lst.classList.add('swap');
    document.body.appendChild(lst);
    const btn = document.createElement('a');
    btn.innerText = '<';
    btn.classList.add('swap');
    btn.addEventListener('click', (e) => {
        lst.classList.toggle('hide');
        e.stopPropagation();
    });
    document.body.appendChild(btn);
    document.body.addEventListener('click', () => {
        lst.classList.add('hide');
    });
});
