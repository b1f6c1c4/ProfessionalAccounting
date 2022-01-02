/* Copyright (C) 2020-2022 b1f6c1c4
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

define('ace/mode/accounting', function(require, exports, module) {
  const oop = require('../lib/oop');
  const TextMode = require('./text').Mode;

  const AccountingHighlightRules = require('./accounting_highlight_rules').AccountingHighlightRules;
  const AccountingFoldMode = require('./folding/accounting').AccountingFoldMode;

  const Mode = function() {
    this.HighlightRules = AccountingHighlightRules;
    this.foldingRules = new AccountingFoldMode();
  };
  oop.inherits(Mode, TextMode);

  Mode.prototype.lineCommentStart = '//';

  exports.Mode = Mode;
});
