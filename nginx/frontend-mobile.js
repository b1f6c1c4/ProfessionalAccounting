const editor = document.getElementById('editor');
const cmdLine = document.getElementById('cmdLine');
cmdLine.editor = editor;
editor.cmdLine = cmdLine;

const freeze = (f) => {
  editor.disabled = f;
  cmdLine.disabled = f;
  document.getElementById('create').disabled = f;
  document.getElementById('upsert').disabled = f;
  document.getElementById('remove').disabled = f;
};

const getRow = (pos) => {
  let st = pos;
  while (st) {
    st--;
    if (editor.value[st] == '\n') {
      st++;
      break;
    }
  }

  let ed = pos;
  while (ed < editor.value.length) {
    if (editor.value[ed] == '\n') {
      ed--;
      break;
    }
    ed++;
  }

  return { st, ed };
};

const finalize = (answer, success, insert) => {
  if (insert) {
    const pos = getRow(editor.selectionStart);
    editor.selectionStart = pos;
    editor.selectionEnd = pos;
  } else {
    editor.value = '';
  }
  editor.setRangeText(answer);
  autosize.update(editor);
  freeze(false);
  if (success) {
    cmdLine.setSelectionRange(0, cmdLine.value.length);
  } else {
    console.error(answer);
  }
};

const prepareObject = () => {
  let { st: sst, ed: sed } = getRow(editor.selectionStart);
  while (!editor.value.substring(sst, sed + 1).match(/^@new [A-Z][a-z]+ \{/)) {
    if (sst) {
      ({ st: sst, ed: sed } = getRow(sst - 1));
    } else {
      return undefined;
    }
  }

  let { st: est, ed: eed } = getRow(sed + 1);
  while (!editor.value.substring(est, eed + 1).match(/\}@$/)) {
    if (eed < editor.value.length) {
      ({ st: est, ed: eed } = getRow(eed + 2));
    } else {
      return undefined;
    }
  }

  const obj = editor.value.substring(sst, eed + 1);
  const rng = { st: sst, ed: eed + 1 };
  return { rng, obj };
};

const indicateResult = (res, { st, ed }) => {
  freeze(false);
  editor.setRangeText(res.endsWith('\n') ? res : res + '\n', st, ed + 1);
  autosize.update(editor);
  editor.selectionStart = st;
  editor.selectionEnd = st;
};

const indicateError = (err, { st, ed }) => {
  freeze(false);
  console.error(err);
  editor.setRangeText(err.endsWith('\n') ? err : err + '\n', ed + 1, ed + 1);
  autosize.update(editor);
  editor.selectionStart = st;
  editor.selectionEnd = st;
};

const doCreate = () => {
  console.log('doCreate');
  freeze(true);
  execute('').then((res) => {
    finalize(res, true, true);
    // editor.renderer.scrollCursorIntoView();
  }).catch((err) => {
    finalize(err, false, true);
  });
  editor.focus();
};

const doUpsert = () => {
  console.log('doUpsert');
  const { rng, obj } = prepareObject();
  freeze(true);
  upsert(obj).then((res) => {
    indicateResult(res, rng);
  }).catch((err) => {
    indicateError(err, rng);
  });
};

const doUpsertAll = async () => {
  console.log('doUpsertAll');
  let st = 0;
  while (true) {
    editor.selectionStart = st;
    editor.selectionEnd = st;
    const { rng, obj } = prepareObject();
    if (rng.ed <= st) {
      break;
    }
    st = rng.ed + 1;
    try {
      freeze(true);
      const res = await upsert(obj);
      indicateResult(res, rng);
    } catch (err) {
      indicateError(err, rng);
      break;
    }
  }
};

const doRemove = () => {
  console.log('doRemove');
  const { rng, obj } = prepareObject();
  freeze(true);
  remove(obj).then((res) => {
    indicateResult(`/*${obj}*/`, rng);
  }).catch((err) => {
    indicateError(err, rng);
  });
};

const doRemoveAll = async () => {
  console.log('doRemoveAll');
  let st = 0;
  while (true) {
    editor.selectionStart = st;
    editor.selectionEnd = st;
    const { rng, obj } = prepareObject();
    if (rng.ed <= st) {
      break;
    }
    st = rng.ed + 1;
    try {
      freeze(true);
      const res = await remove(obj);
      indicateResult(res, rng);
    } catch (err) {
      indicateError(err, rng);
      break;
    }
  }
};

const doUpload = () => {
  console.log('doUpload');
  const command = cmdLine.value;
  const text = editor.value;
  const combined = command + '\n' + text;
  freeze(true);
  execute(combined).then((res) => {
    finalize(res, true, false);
    editor.focus();
    // editor.renderer.scrollCursorIntoView();
  }).catch((err) => {
    finalize(err, false, false);
    // editor.renderer.scrollCursorIntoView();
  });
};

const doExecuteFactory = (app) => () => {
  console.log('doExecuteFactory(' + app + ')');
  const command = cmdLine.value;
  if (command === '') {
    doCreate();
    return;
  }
  freeze(true);
  execute(command).then((res) => {
    finalize(res, true, app);
    editor.focus();
    // editor.renderer.scrollCursorIntoView();
  }).catch((err) => {
    finalize(err, false, app);
    // editor.renderer.scrollCursorIntoView();
  });
};

editor.onkeydown = (e) => {
  if (e.keyCode == 10 || e.keyCode == 13) {
    if (!e.ctrlKey && e.altKey && !e.shiftKey) {
      e.preventDefault();
      doUpsert();
    } else if (e.ctrlKey && e.altKey && !e.shiftKey) {
      e.preventDefault();
      doUpload();
    } else if (e.ctrlKey && e.altKey && e.shiftKey) {
      e.preventDefault();
      doUpsertAll();
    }
  } else if (e.keyCode == 46) {
    if (!e.ctrlKey && e.altKey && !e.shiftKey) {
      e.preventDefault();
      doRemove();
    } else if (e.ctrlKey && e.altKey && e.shiftKey) {
      e.preventDefault();
      doRemoveAll();
    }
  } else if (e.keyCode == 9) {
    e.preventDefault();
    cmdLine.focus();
  }
};
editor.value = notice;

cmdLine.onkeydown = (e) => {
  if (e.keyCode == 9) {
    e.preventDefault();
    editor.focus();
  } else if (e.keyCode == 10 || e.keyCode == 13) {
    if (e.ctrlKey && e.altKey && !e.shiftKey) {
      e.preventDefault();
      doUpload();
    } else if (!e.ctrlKey && !e.altKey && e.shiftKey) {
      e.preventDefault();
      doExecuteFactory(true)();
    } else if (!e.ctrlKey && !e.altKey && !e.shiftKey) {
      e.preventDefault();
      doExecuteFactory(false)();
    }
  }
};
cmdLine.focus();

document.getElementById('create').onclick = doCreate;
document.getElementById('upsert').onclick = doUpsert;
document.getElementById('remove').onclick = doRemove;
document.getElementById('upload').onclick = doUpload;
