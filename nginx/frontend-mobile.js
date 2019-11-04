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

const finalize = (answer, success, insert) => {
  if (insert) {
    // editor.setSelectionRange({
    //   start: { row: editor.selection.getCursor().row, column: 0 },
    //   end: { row: editor.selection.getCursor().row, column: 0 },
    // }, false);
  } else {
    editor.value = '';
  }
  // const original = editor.selection.getCursor();
  // editor.insert(answer);
  // editor.setSelectionRange({
  //   start: original,
  //   end: original,
  // }, false);
  freeze(false);
  if (success) {
    cmdLine.setSelectionRange(0, cmdLine.value.length);
  } else {
    console.error(answer);
  }
};

const prepareObject = () => {
  // let row = editor.selection.getCursor().row;
  // while (!editor.session.doc.getLine(row).match(/^@new [A-Z][a-z]+ \{/)) {
  //   if (row) {
  //     row--;
  //   } else {
  //     return undefined;
  //   }
  // }
  // const st = row;
  // while (!editor.session.doc.getLine(row).match(/\}@$/)) {
  //   if (row < editor.session.doc.getLength()) {
  //     row++;
  //   } else {
  //     return undefined;
  //   }
  // }
  // const ed = row;
  // const obj = editor.session.doc.getLines(st, ed).join('\n');
  // const rng = new ace.Range(st, 0, ed + 1, 0);
  // return { rng, obj };
};

const indicateResult = (res, rng) => {
  freeze(false);
  // editor.replace(rng, res.endsWith('\n') ? res : res + '\n');
};

const indicateError = (err, { end }) => {
  freeze(false);
  console.error(err);
  // editor.session.doc.insert(end, err.endsWith('\n') ? err : err + '\n');
};

const doCreate = () => {
  console.log('doCreate');
  freeze(true);
  execute('').then((res) => {
    finalize(res, true, true);
    // editor.renderer.scrollCursorIntoView();
    // editor.selection.setSelectionRange({
    //   start: { row: editor.selection.getCursor().row + 1, column: 0 },
    //   end: { row: editor.selection.getCursor().row + 1, column: 0 },
    // }, false);
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
  let rid = 0;
  while (true) {
    // editor.selection.setSelectionRange({
    //   start: { row: rid, column: 0 },
    //   end: { row: rid, column: 0 },
    // }, false);
    const { rng, obj } = prepareObject();
    // if (rng.end.row <= rid) {
    //   break;
    // }
    // rid = rng.end.row;
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
  let rid = 0;
  while (true) {
    // editor.selection.setSelectionRange({
    //   start: { row: rid, column: 0 },
    //   end: { row: rid, column: 0 },
    // }, false);
    const { rng, obj } = prepareObject();
    // if (rng.end.row <= rid) {
    //   break;
    // }
    // rid = rng.end.row;
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
    e.preventDefault();
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
