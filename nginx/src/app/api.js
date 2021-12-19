/* Copyright (C) 2020-2021 Iori Oikawa
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
    method: 'POST',
    responseType: 'text',
});

const createHeaders = (user) => ({
    'X-ClientDateTime': dayjs().format(),
    'X-User': user,
    'X-Limit': 10,
});

export async function safeApi(expr, user) {
    const { data, headers } = await instance.request({
        method: 'GET',
        url: '/safe',
        params: { q: expr },
        headers: createHeaders(user),
    });
    const type = headers['X-Type'];
    const autoReturn = headers['X-AutoReturn'] === 'true';
    const dirty = headers['X-Dirty'] === 'true';
    return { data, type, autoReturn, dirty };
}

export async function executeApi(expr, user) {
    const { data, headers } = await instance.request({
        url: '/execute',
        data: expr,
        headers: createHeaders(user),
    });
    const type = headers['X-Type'];
    const autoReturn = headers['X-AutoReturn'] === 'true';
    const dirty = headers['X-Dirty'] === 'true';
    return { data, type, autoReturn, dirty };
}

export async function voucherUpsertApi(code, user) {
    const { data } = await instance.request({
        url: '/voucherUpsert',
        data: code,
        headers: createHeaders(user),
    });
    return { data };
}

export async function voucherRemovalApi(code, user) {
    await instance.request({
        url: '/voucherRemoval',
        data: code,
        headers: createHeaders(user),
    });
}

export async function assetUpsertApi(code, user) {
    const { data } = await instance.request({
        url: '/assetUpsert',
        data: code,
        headers: createHeaders(user),
    });
    return { data };
}

export async function assetRemovalApi(code, user) {
    await instance.request({
        url: '/assetRemoval',
        data: code,
        headers: createHeaders(user),
    });
}

export async function amortUpsertApi(code, user) {
    const { data } = await instance.request({
        url: '/amortUpsert',
        data: code,
        headers: createHeaders(user),
    });
    return { data };
}

export async function amortRemovalApi(code, user) {
    await instance.request({
        url: '/amortRemoval',
        data: code,
        headers: createHeaders(user),
    });
}
