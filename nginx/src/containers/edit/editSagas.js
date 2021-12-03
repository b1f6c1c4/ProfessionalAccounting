import { call, put, select, takeEvery, takeLatest } from 'redux-saga/effects';
import * as Api from '../../app/api.js';
import {
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
    revertVoucherRequested,
    revertVoucherSucceeded,
    revertVoucherFailed,
} from './editSlice.js';

const theUser = window.localStorage.getItem('user') || 'anonymous';

function* submitVoucher(action) {
    try {
        const code = yield select((state) => state.edit.liveViewText);
        let { data } = yield call(Api.voucherUpsertApi, code, theUser);
        data = data.trim();
        data = data.substr(1, data.length - 2);
        yield put(submitVoucherSucceeded(data));
    } catch (e) {
        if (e.response) {
            yield put(submitVoucherFailed(e.message + '\n' + e.response.data));
        } else {
            yield put(submitVoucherFailed(e.message));
        }
    }
}

function* revertVoucher(action) {
    try {
        const code = yield select((state) => state.edit.liveViewText);
        yield call(Api.voucherRemovalApi, code, theUser);
        yield put(revertVoucherSucceeded());
    } catch (e) {
        if (e.response) {
            yield put(revertVoucherFailed(e.message + '\n' + e.response.data));
        } else {
            yield put(revertVoucherFailed(e.message));
        }
    }
}

export default function* () {
    yield takeEvery(submitVoucherRequested, submitVoucher);
    yield takeEvery(revertVoucherRequested, revertVoucher);
}
