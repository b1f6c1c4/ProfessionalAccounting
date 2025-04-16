/* Copyright (C) 2020-2025 b1f6c1c4
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

grammar Subtotal;

/*
 * Parser Rules
 */

subtotal
	:	WS* Mark=SumMark subtotalFields? subtotalAggr? subtotalEqui?
	|	WS* Mark=CountMark subtotalFields? subtotalAggr?
	;

subtotalFields
	:	SubtotalNoField
	|	subtotalField+
	;

subtotalField
	:	SubtotalField SubtotalFieldZ?
	;

subtotalAggr
	:	AggrMark AllDate?
	|	AggrMark SquareBra rangeCore SquareKet
	;

subtotalEqui
	:	EquiMark VoucherCurrency? (SquareBra rangeDay SquareKet)?
	;

rangeCore
	:	RangeNull | RangeAllNotNull
	|	Begin=rangeCertainPoint Tilde End=rangeCertainPoint?
	|	Tilde End=rangeCertainPoint
	|	Certain=rangeCertainPoint
	;

rangeCertainPoint
	:	rangeYear
	|	rangeQuarter
	|	rangeMonth
	|	rangeWeek
	|	rangeDay
	;

rangeYear
	:	RangeAYear
	;

rangeQuarter
	:	(RangeAQuarter | RangeDeltaQuarter)
	;

rangeMonth
	:	(RangeAMonth | RangeDeltaMonth)
	;

rangeWeek
	:	RangeDeltaWeek
	;

rangeDay
	:	RangeADay | RangeDeltaDay
	;

/*
 * Lexer Rules
 */

SumMark
	:	'`' | '``'
	;

CountMark
	:	'!' | '!!'
	;

SubtotalField
	:	[RKtscrdwmqyCUV]
	;

SubtotalNoField
	:	'v'
	;

SubtotalFieldZ
	:	'z'
	;

RangeNull
	:	'null'
	;
RangeAllNotNull
	:	'~null'
	;

RangeAYear
	:	'20' [1-2] [0-9]
	;
RangeAQuarter
	:	'20' [1-2] [0-9] 'Q' [1-4]
	;
RangeDeltaQuarter
	:	'Q' '0'
	|	'Q' '-' [1-9] [0-9]*
	;
RangeAMonth
	:	'20' [1-2] [0-9] '0' [1-9]
	|	'20' [1-2] [0-9] '1' [0-2]
	;
RangeDeltaMonth
	:	'0'
	|	'-' [1-9] [0-9]*
	;
RangeADay
	:	'20' [1-2] [0-9] '0' [1-9] [0-3] [0-9]
	|	'20' [1-2] [0-9] '1' [0-2] [0-3] [0-9]
	;
RangeDeltaDay
	:	'.'+
	;
RangeDeltaWeek
	:	','+
	;

VoucherCurrency
	:	'@' [a-zA-Z]+
	|	'@@'
	;

SquareBra
	:	'['
	;
SquareKet
	:	']'
	;

AllDate
	:	'[]'
	;

Tilde
	:	'~' | '~~'
	;

AggrMark
	:	[DWMQY]
	;
EquiMark
	:	'X'
	;

WS
	:	[ \n\r\t]
	;
