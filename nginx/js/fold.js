/* Copyright (C) 2020-2024 b1f6c1c4
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

define('ace/mode/folding/accounting', function(require, exports, module) {
  const oop = require('../../lib/oop');
  const Range = require('../../range').Range;
  const BaseFoldMode = require('./fold_mode').FoldMode;

  const FoldMode = function() {
    this.foldingStartMarker = /^(@new [A-Z][a-z]+ )(\{)/;
    this.foldingStopMarker = /\}@$/;
    this.getFoldWidgetRange = function(session, foldStyle, row) {
      const line = session.getLine(row);
      const match = line.match(this.foldingStartMarker);
      if (match) {
        const i = match.index + match[1].length;
        return this.openingBracketBlock(session, match[2], row, i);
      }
    };
  };

  oop.inherits(FoldMode, BaseFoldMode);
  exports.AccountingFoldMode = FoldMode;
});
