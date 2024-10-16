/* Copyright (C) 2024 b1f6c1c4
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

"use strict";

require('tuicss/dist/tuicss.min.js');

// JavaScript for Accounting System
const theOrders = []; // Array to hold order objects
let currOrderIndex = 0; // Current order index

// Constants for API headers
const API_URL = process.env.API_URL || ''; // Set your API base URL here
const user = localStorage.getItem('user') || 'anonymous'; // Get user from localStorage
const headers = () => {
    const d = new Date();
    const ld = new Date(+d - 1000*60*d.getTimezoneOffset());
    const ldt = ld.toISOString().replace(/Z$/, '');
    return {
        'X-User': user,
        'X-ClientDateTime': ldt,
    };
};

// Helper function to make API calls with proper headers
async function apiCall(url, options = {}) {
    options.headers = {
        ...options.headers,
        ...headers()
    };
    const response = await fetch(`${API_URL}${url}`, options);
    if (!response.ok) {
        throw new Error(await response.text());
    }
    return options.json ? response.json() : response.text();
}

// Predefined options
let predefinedCategories = [];
let predefinedUsers = [];
let predefinedPaymentMethods = [];

// Elements
const previewTextarea = document.getElementById('preview');
const saveBtn = document.getElementById('saveBtn');
const rescindBtn = document.getElementById('rescindBtn');
const customCategoryInput = document.getElementById('customCategory');
const paymentMethodInput = document.getElementById('paymentMethod');
const paymentMethodsDatalist = document.getElementById('paymentMethods');
const categoriesDatalist = document.getElementById('categories');
const beneficiarySelect = document.getElementById('beneficiary');
const payerSelect = document.getElementById('payer');

function showError(e) {
    document.querySelector('#errorModal pre').innerText = e;
    document.querySelector('#errorModal .tui-modal-button').click();
    document.querySelector('#errorModal .tui-modal-close-button').focus();
}

let isSaved = undefined;
// true: saved, never touched
// null: being touched
// false: never saved
function setIsSaved(v) {
    const dis = (b, d) => {
        b.disabled = d;
        b.classList.toggle('disabled', d);
        b.classList.toggle('white-168', d);
    };
    if (v === null) {
        if (isSaved) {
            dis(saveBtn, false);
            dis(rescindBtn, false);
        } else {
            dis(saveBtn, false);
            dis(rescindBtn, true);
        }
        previewTextarea.style.color = 'orange';
    } else {
        if (v) {
            dis(saveBtn, true);
            dis(rescindBtn, false);
            previewTextarea.style.color = '#00ff00';
        } else {
            dis(saveBtn, false);
            dis(rescindBtn, true);
            previewTextarea.style.removeProperty('color');
        }
        isSaved = v;
    }
}

document.addEventListener('keydown', (e) => {
    if (!e.altKey)
        return;
    if (e.key === 'Enter') {
        e.preventDefault();
        handleSave();
    } else if (e.key === 'Delete') {
        e.preventDefault();
        if (isSaved === true)
            handleRescind();
    } else if (e.key === 'ArrowLeft') {
        navigateOrder(currOrderIndex - 1);
    } else if (e.key === 'ArrowRight') {
        navigateOrder(currOrderIndex + 1);
    }
});

// Initial data fetching
document.addEventListener('DOMContentLoaded', async () => {
    await fetchPredefinedOptions();
});

// Fetch predefined categories, beneficiaries, and payers
async function fetchPredefinedOptions() {
    try {
        const categories = await apiCall('/api/safe?q=raw%20U%20%3E%20%25taobao-%25.*!tscr', {
            headers: { 'X-Limit': 50 },
        });
        const paymentMethods = await apiCall('/api/safe?q=raw%20U%20%3C%20%25taobao-%25.*!tscr', {
            headers: { 'X-Limit': 50 },
        });
        const beneficiaryPayerData = await apiCall('/api/safe?q=json%20U!U', {
            headers: { 'X-Limit': 50 },
            json: true,
        });
        const process = (ss) => ss.split('\n')
            .filter((s) => s.startsWith('T'))
            .map((s) => s.replace(/  [0-9.]+$/, ''));

        predefinedCategories = process(categories);
        predefinedUsers = Object.keys(beneficiaryPayerData.user);
        predefinedPaymentMethods = process(paymentMethods);

        payerSelect.innerHTML = getOptionHtml(predefinedUsers);
        paymentMethodsDatalist.innerHTML = getOptionHtml(predefinedPaymentMethods, true);
        categoriesDatalist.innerHTML = getOptionHtml(predefinedCategories, true);
    } catch (e) {
        showError(e);
    }
}

// Helper function to populate select dropdowns
function getOptionHtml(optionsArray, isDatalist) {
    let html = '';
    optionsArray.forEach(option => {
        const enc = option.replace(/[&<>"']/g, c => `&#${c.charCodeAt(0)};`);
        html += `<option value="${enc}">${isDatalist ? '' : enc}</option>\n`;
    });
    return html;
}

// Add event listeners for the buttons
document.getElementById('lastUnsaved').addEventListener('click', () => navigateToSpecialOrder('backward', false));
document.getElementById('lastSaved').addEventListener('click', () => navigateToSpecialOrder('backward', true));
document.getElementById('lastOrder').addEventListener('click', () => navigateOrder(currOrderIndex - 1));
document.getElementById('nextOrder').addEventListener('click', () => navigateOrder(currOrderIndex + 1));
document.getElementById('nextSaved').addEventListener('click', () => navigateToSpecialOrder('forward', true));
document.getElementById('nextUnsaved').addEventListener('click', () => navigateToSpecialOrder('forward', false));
document.getElementById('nextBtn').addEventListener('click', () => navigateOrder(currOrderIndex + 1));

previewTextarea.addEventListener('input', () => setIsSaved(null));

// Function to navigate to the next or previous saved/unsaved order
async function navigateToSpecialOrder(direction, saved) {
    let found = false;
    let newIndex = currOrderIndex;
    document.querySelector('#searchModal .tui-modal-button').click();
    const pg = document.querySelector('#searchModal .tui-progress')

    const inc = direction === 'forward' ? 1 : -1;
    for (let i = 1; i <= theOrders.length; i++) {
        pg.style.width = `${100 * i / theOrders.length}%`;
        const id = (currOrderIndex + theOrders.length + i * inc) % theOrders.length;
        if (saved === !!await fetchVoucher(theOrders[id])) {
            newIndex = id;
            found = true;
            break;
        }
    }

    document.querySelector('#searchModal .tui-modal-close-button').click();
    if (found) {
        navigateOrder(newIndex);
    } else {
        showError('No such orders found');
        return false;
    }
    return true;
}

// Helper function to check if the order at the given index is saved
async function fetchVoucher(order) {
    const orderNumber = order.orderNumber;

    // Perform an API call to check if the order exists in the database
    const apiUrl = `/api/safe?q=U%20%taobao-${orderNumber}%`;

    return apiCall(apiUrl);
}

async function navigateOrder(newIndex) {
    if (newIndex === -1)
        newIndex = theOrders.length - 1;
    else if (newIndex === theOrders.length)
        newIndex = 0;
    if (newIndex < 0 || newIndex >= theOrders.length) {
        showError('Invalid order index');
        return;
    }

    // Update current order index
    currOrderIndex = newIndex;

    // Get the current order based on the new index
    const currentOrder = theOrders[currOrderIndex];

    // Fill out the order details in the UI
    document.getElementById('orderIndex').innerText = `${newIndex + 1}/${theOrders.length}`;
    document.getElementById('orderNumber').value = currentOrder.orderNumber;
    document.getElementById('orderDate').value = currentOrder.orderDate;
    document.getElementById('storeName').value = currentOrder.storeName;
    document.getElementById('orderStatus').value = currentOrder.orderStatus;
    document.getElementById('orderStatus').classList.toggle('warning', currentOrder.orderStatus !== '交易成功');
    document.getElementById('orderDiff').value = `¥${currentOrder.orderDiff.toFixed(2)}`;
    document.getElementById('orderDiff').classList.toggle('warning', currentOrder.orderDiff !== 0);
    document.getElementById('orderAmount').value = `¥${currentOrder.orderAmount.toFixed(2)}`;

    // Clear the items table body
    const itemsBody = document.getElementById('itemsBody');
    itemsBody.innerHTML = ''; // Clear existing rows

    // Fill out the item details in the table
    currentOrder.items.forEach((item, id) => {
        const row = document.createElement('tr');
        row.setAttribute('data-id', id);
        let html = `
            <td><a tabindex="-1" href=${item.link} target="_blank">${item.name}</a></td>
            <td>${item.style}</td>
            <td>¥${item.price.toFixed(2)}</td>
            <td>${item.quantity}</td>
        `;
        if (item.refund)
            html += `
                <td class="warning" colspan="2">Refunded</td>
            `;
        else
            html += `
                <td>
                    <select class="tui-input full-width">
                        ${getOptionHtml(predefinedUsers)}
                    </select>
                </td>
                <td>
                    <input class="tui-input full-width" list="categories">
                </td>
            `;
        row.innerHTML = html;
        itemsBody.appendChild(row);
        row.querySelectorAll('select, input')
            .forEach(s => s.addEventListener('input', compilePreviewContent));
    });

    paymentMethodInput.focus();

    // Inspect if the order is already saved in the database
    const voucher = await fetchVoucher(currentOrder);
    if (voucher) {
        // If the order is found in the database, populate the textarea with the returned content
        previewTextarea.value = voucher;
        setIsSaved(true);
    } else {
        setIsSaved(false);
        // If the order is not found, compile the content from user inputs
        compilePreviewContent();
    }
}

// Compile preview content based on the selected or input data
function compilePreviewContent() {
    if (isSaved === true) {
        showError('You must rescind first if you want to make chages.');
        return;
    }
    const orderData = theOrders[currOrderIndex]; // Get the current order data
    const payer = document.getElementById('payer').value;
    const paymentMethod = document.getElementById('paymentMethod').value;

    let previewContent = `@new Voucher {\n! ${orderData.orderDate.replace(/-/g, '')} %taobao-${orderData.orderNumber}% @CNY\n`;

    // Iterate through order items
    const body = document.getElementById('itemsBody');
    orderData.items.forEach((item, id) => {
        if (item.refund) {
            previewContent += `U${payer} 2s G() : ${item.price}*${item.quantity} ;\n`;
            return;
        }
        const row = body.children[id];
        const beneficiary = row.querySelector('select').value;
        const category = row.querySelector('input').value;
        previewContent += `U${beneficiary} ${category} : ${item.price}*${item.quantity} ;\n`;
    });
    if (orderData.orderDiff < 0)
        previewContent += `d${-orderData.orderDiff}`;
    else
        previewContent += `t${orderData.orderDiff}`;
    previewContent += `\nU${payer} @CNY ${paymentMethod} -${orderData.orderAmount}\n}@`;
    previewTextarea.value = previewContent;
    setIsSaved(false);
}

// Handle file upload button click
document.getElementById('uploadForm').addEventListener('submit', handleCsvUpload);

// Handle CSV file upload and process it
async function handleCsvUpload(e) {
    e.preventDefault();
    const csvFileInput = document.getElementById('csvFile');
    const file = csvFileInput.files[0];

    if (file) {
        const reader = new FileReader();
        reader.onload = async function (e) {
            const csvContent = e.target.result;
            processCsvData(csvContent);
            currOrderIndex = -1;
            if (!await navigateToSpecialOrder('forward', false))
                navigateOrder(0);
        };
        reader.readAsText(file);
    }
}

// Process CSV data and display in UI
function processCsvData(csvData) {
    const rows = csvData.split('\n').slice(1); // Split CSV into rows and remove the header row
    let currentOrder = null;
    const push = () => {
        if (!currentOrder)
            return;
        const diff = currentOrder.orderAmount - currentOrder.orderTotal;
        if (Math.abs(diff) < 1e-9)
            currentOrder.orderDiff = 0;
        else
            currentOrder.orderDiff = diff;
        theOrders.push(currentOrder);
    };

    rows.forEach(row => {
        if (!row.trim()) return; // Skip empty rows

        const columns = row.split(','); // Split row into columns

        // Extract relevant columns from the CSV row
        const orderNumber = columns[0]; // 订单编号
        const orderDate = columns[1]; // 下单日期
        const storeName = columns[2]; // 店铺名称
        const itemName = columns[3]; // 商品名称
        const itemStyle = columns[4]; // 商品分类
        const itemPrice = parseFloat(columns[8]); // 单价
        const itemQuantity = parseInt(columns[9]); // 数量
        const itemLink = columns[6]; // 商品链接
        const itemRefund = !!columns[10]; // 退款状态
        const orderAmount = parseFloat(columns[11]); // 实付款
        const orderStatus = columns[12]; // 交易状态

        if (!itemPrice)
            return;

        // Check if we are still within the same order (consecutive rows with the same order number)
        if (!currentOrder || currentOrder.orderNumber !== orderNumber) {
            // If a new order starts, push the previous order (if exists) and start a new one
            push();

            currentOrder = {
                orderNumber,
                orderDate,
                orderStatus,
                orderAmount,
                orderTotal: 0,
                storeName,
                items: []
            };
        }

        // Add item to the current order's items list
        currentOrder.items.push({
            name: itemName,
            style: itemStyle,
            price: itemPrice,
            quantity: itemQuantity,
            link: itemLink,
            refund: itemRefund,
        });
        currentOrder.orderTotal += itemPrice * itemQuantity;
    });

    // Push the last order after the loop finishes
    push();
}

function getVoucher() {
    return previewTextarea.value.replace(/^\s*@/m, '').replace(/@\s*$/m, '');
}

// Handle Save button click
async function handleSave() {
    const pd = (list, v) => {
        const t = v.trim();
        if (!list.includes(t))
            list.splice(0, 0, t);
    };
    pd(predefinedPaymentMethods, paymentMethodInput.value);
    document.querySelectorAll('#itemsBody input').forEach(i => pd(predefinedCategories, i.value));
    paymentMethodsDatalist.innerHTML = getOptionHtml(predefinedPaymentMethods, true);
    categoriesDatalist.innerHTML = getOptionHtml(predefinedCategories, true);
    try {
        previewTextarea.value = await apiCall('/api/voucherUpsert', {
            method: 'POST',
            headers: {
                'Content-Type': 'text/plain'
            },
            body: getVoucher(),
        });
        setIsSaved(true);
    } catch (e) {
        showError(e);
    }
}
saveBtn.addEventListener('click', handleSave);

// Handle Rescind button click
async function handleRescind() {
    try {
        await apiCall('/api/voucherRemoval', {
            method: 'POST',
            headers: {
                'Content-Type': 'text/plain'
            },
            body: getVoucher(),
        });
        isSaved = false;
        compilePreviewContent();
    } catch (e) {
        showError(e);
    }
}
rescindBtn.addEventListener('click', handleRescind);

// Monitor field changes and update preview content
payerSelect.addEventListener('input', compilePreviewContent);
paymentMethodInput.addEventListener('input', compilePreviewContent);
