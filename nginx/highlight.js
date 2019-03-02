define('ace/mode/accounting_highlight_rules', function(require, exports, module) {
  const oop = require('../lib/oop');
  const TextHighlightRules = require('./text_highlight_rules').TextHighlightRules;

  const AccountingHighlightRules = function() {
    this.$rules = {
      start: [{
        token: ['markup.bold', 'keyword.control'],
        regex: /^(\/\*)?(@new (?:Voucher|Asset|Amortization) \{)/,
        next: 'obj',
      }, {
        token: 'constant.numeric.date',
        regex: /(?:(?![.,]).|^)[1-2][0-9][0-9][0-9][0-1][0-9][0-3][0-9](?![.,])/,
      }, {
        token: 'variable.parameter.currency',
        regex: /(?:@|\b)[A-Z][A-Z][A-Z]\b/,
      }, {
        token: 'markup.list.numbered.title',
        regex: /\b(?:(?![.,]).|^)[12346][0-9][0-9][0-9](?![.,])\b/,
      }, {
        token: 'markup.list.numbered.subtitle',
        regex: /\b(?:(?![.,]).|^)[0-9][0-9](?![.,])\b/,
      }, {
        token: 'markup.bold.numeric',
        regex: /(?:(?!\w).|^)[$¥€]?[-+]?(?:\.[0-9]+|[0-9,]+\.?[0-9]*)(?:[eE][+-]?[0-9]+)?\b/,
      }, {
        token: 'string.single',
        regex: /'(?:[^']+|'')*'/,
      }, {
        token: 'string.double',
        regex: /"(?:[^"]+|"")*"/,
      }, {
        token: 'comment',
        regex: /\/\/.*$/,
      }, {
        defaultToken : 'text',
      }],
      obj: [{
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
        token: 'constant.numeric.date',
        regex: /(?:(?![.,]).|^)[1-2][0-9][0-9][0-9][0-1][0-9][0-3][0-9](?![.,])/,
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
        token: 'string.interpolated',
        regex: /\^[0-9a-f]+\^/,
      }, {
        token: 'string.single',
        regex: /'(?:[^']+|'')*'/,
      }, {
        token: 'string.double',
        regex: /@?"(?:[^"]+|"")*"/,
      }, {
        token: 'string.quoted',
        regex: /%(?:[^%]+|%%)*%/,
      }, {
        token: 'entity.name.function',
        regex: /\b[A-Z][a-zA-Z]*\b/,
      }, {
        token: 'comment',
        regex: /\/\/.*$/,
      }, {
        defaultToken : 'text',
      }],
    };

    this.normalizeRules();
  };

  oop.inherits(AccountingHighlightRules, TextHighlightRules);

  exports.AccountingHighlightRules = AccountingHighlightRules;
});
