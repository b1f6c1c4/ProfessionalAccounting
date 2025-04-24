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

import axios from 'axios';
import dayjs from 'dayjs';

const instance = axios.create({
    baseURL: process.env.API_URL ?? '/api',
    responseType: 'text',
});

export async function safeApi(expr, user) {
    const { data, headers } = await instance.request({
        method: 'GET',
        url: '/safe',
        params: { q: expr, limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
    return { data };
}

export async function executeApi(expr, user) {
    const { data, headers } = await instance.request({
        method: 'POST',
        url: '/execute',
        data: expr,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
    return { data };
}

export async function voucherUpsertApi(code, user) {
    const { data } = await instance.request({
        method: 'POST',
        url: '/voucher',
        data: code,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
    return { data };
}

export async function voucherRemovalApi(code, user) {
    await instance.request({
        method: 'DELETE',
        url: '/voucher',
        data: code,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
}

export async function assetUpsertApi(code, user) {
    const { data } = await instance.request({
        method: 'POST',
        url: '/asset',
        data: code,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
    return { data };
}

export async function assetRemovalApi(code, user) {
    await instance.request({
        method: 'DELETE',
        url: '/asset',
        data: code,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
}

export async function amortUpsertApi(code, user) {
    const { data } = await instance.request({
        method: 'POST',
        url: '/amort',
        data: code,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
    return { data };
}

export async function amortRemovalApi(code, user) {
    await instance.request({
        method: 'DELETE',
        url: '/amort',
        data: code,
        params: { limit: 10, u: user },
        headers: { 'X-ClientDateTime': dayjs().format() },
    });
}
