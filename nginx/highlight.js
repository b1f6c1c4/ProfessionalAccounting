define('ace/mode/accounting_highlight_rules', function(require, exports, module) {
  const oop = require('../lib/oop');
  const TextHighlightRules = require('./text_highlight_rules').TextHighlightRules;

  const AccountingHighlightRules = function() {
    this.$rules = {
      start: [{
        token: 'comment',
        regex: /\/\/.*$/,
      }, {
        token: 'string.single',
        regex: /'(?:[^']|'')*'/,
      }, {
        token: 'string.double',
        regex: /"(?:[^"]|"")*"/,
      }, {
        token: 'text',
        regex: /[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}/,
      }, {
        token: ['markup.bold', 'keyword.control'],
        regex: /^(\/\*)?(@new (?:Voucher|Asset|Amortization) \{)/,
        next: 'obj',
      }, {
        token: 'variable.parameter.user',
        regex: /\bU[A-Za-z0-9_]+(&[A-Za-z0-9_]+)*\b/,
      }, {
        token: 'variable.parameter.currency',
        regex: /@[A-Z][A-Z][A-Z]\b/,
      }, {
        token: 'variable.parameter.currency',
        regex: /\b[A-Z][A-Z][A-Z](?=\s|$)/,
      }, {
        token: 'markup.bold.numeric',
        regex: /[$¥€][-+]?(?:\.[0-9]+|[0-9,]+\.?[0-9]*)(?:[eE][+-]?[0-9]+)?\b/,
      }, {
        token: 'constant.numeric.date',
        regex: /\b[1-2][0-9][0-9][0-9][0-1][0-9][0-3][0-9](?![-.,])/,
      }, {
        token: 'markup.list.numbered.title',
        regex: /\b[12346][0-9][0-9][0-9](?:[0-9][0-9])?(?![-.,])\b/,
      }, {
        token: 'markup.list.numbered.subtitle',
        regex: /\b[0-9][0-9](?![-.,])\b/,
      }, {
        token: 'markup.bold.numeric',
        regex: /\b-?(?:\.[0-9]+|[0-9]+(?:,[0-9]{3})*\.?[0-9]*)(?:[eE][+-]?[0-9]+)?(?=\s|\b|$)/,
      }, {
        defaultToken : 'text',
      }],
      obj: [{
        token: 'comment',
        regex: /\/\/.*$/,
      }, {
        token: 'string.interpolated',
        regex: /\^[0-9a-f]+\^/,
      }, {
        token: 'string.single',
        regex: /'(?:[^']|'')*'/,
      }, {
        token: 'string.double',
        regex: /@?"(?:[^"]|"")*"/,
      }, {
        token: 'string.quoted',
        regex: /%(?:[^%]|%%)*"/,
      }, {
        token: ['keyword.control', 'markup.bold'],
        regex: /(\}@)(\*\/)?/,
        next: 'start',
      }, {
        token: ['entity.name.function', 'keyword.operator', 'constant.numeric.date', 'keyword.operator'],
        regex: /\b(D)(\()('[1-2][0-9][0-9][0-9]-[0-1][0-9]-[0-3][0-9]')(\))/,
      }, {
        token: ['entity.name.function', 'text', 'keyword.operator', 'text', 'variable.parameter.currency'],
        regex: /\b(Currency)(\s*)(=)(\s*)(@?'[A-Z][A-Z][A-Z]')/,
      }, {
        token: ['entity.name.function', 'text', 'keyword.operator', 'text', 'markup.list.numbered.title'],
        regex: /\b(Title)(\s*)(=)(\s*)([12346][0-9][0-9][0-9])/,
      }, {
        token: ['entity.name.function', 'text', 'keyword.operator', 'text', 'markup.list.numbered.subtitle'],
        regex: /\b(SubTitle)(\s*)(=)(\s*)([0-9][0-9])/,
      }, {
        token: 'keyword.operator',
        regex: /(?:[()=.,<>{}]|\bnew\b)/,
      }, {
        token: 'support.type',
        regex: /^(?:Ordinary|General|Carry|Depreciation|Devalue|AnnualCarry|Uncertain|Amortization)$/,
      }, {
        token: 'support.class',
        regex: /\b(?:Voucher|Asset|Amortization|VoucherDetail|List|AcquisationItem|DepreciateItem|DevalueItem|DisposeItem|AmortItem)\b/,
      }, {
        token: 'variable.parameter.user',
        regex: /\bU[A-Za-z0-9_]+(&[A-Za-z0-9_]+)*\b/,
      }, {
        token: 'variable.parameter.currency',
        regex: /@[a-zA-Z][a-zA-Z][a-zA-Z]\b/,
      }, {
        token: 'markup.list.numbered.title',
        regex: /\bT[12346][0-9][0-9][0-9](?:[0-9][0-9])?\b/,
      }, {
        token: 'support.constant.null',
        regex: /[-+]?(?:\.[0-9]+|[0-9]+\.?[0-9]*)(?:[eE][+-]?[0-9]+)?\b/,
      }, {
        token: 'support.constant.null',
        regex: /\bnull\b/,
      }, {
        token: 'entity.name.function',
        regex: /\b[A-Z][a-zA-Z0-9]*\b/,
      }, {
        token: 'constant.numeric.date',
        regex: /(?:(?![.,]).|^)[1-2][0-9][0-9][0-9][0-1][0-9][0-3][0-9](?![.,])/,
      }, {
        defaultToken : 'text',
      }],
    };

    this.normalizeRules();
  };

  oop.inherits(AccountingHighlightRules, TextHighlightRules);

  exports.AccountingHighlightRules = AccountingHighlightRules;
});
