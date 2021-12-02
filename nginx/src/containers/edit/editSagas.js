import { call, put, select, takeEvery, takeLatest } from 'redux-saga/effects';
import * as Api from '../../app/api.js';
import {
    submitVoucherRequested,
    submitVoucherSucceeded,
    submitVoucherFailed,
} from './editSlice.js';

function* submitVoucher(action) {
    try {
        const code = yield select((state) => state.edit.liveViewText);
        const user = yield select((state) => state.edit.person);
        const text = yield call(Api.voucherUpsertApi, code, user);
        yield put(submitVoucherSucceeded(text));
    } catch (e) {
        yield put(submitVoucherFailed(e.message));
    }
}

export default function* () {
    yield takeEvery(submitVoucherRequested, submitVoucher);
}
