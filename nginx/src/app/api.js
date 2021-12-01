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
