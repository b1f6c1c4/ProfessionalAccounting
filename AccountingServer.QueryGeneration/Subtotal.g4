/* Copyright (C) 2020-2021 b1f6c1c4
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
	:	Mark=SumMark subtotalFields? subtotalAggr? subtotalEqui?
	|	Mark=CountMark subtotalFields? subtotalAggr?
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
	|	rangeMonth
	|	rangeWeek
	|	rangeDay
	;

rangeYear
	:	RangeAYear
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
	:	[tscrdwmyCU]
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
	:	'201' [0-9]
	;
RangeAMonth
	:	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9]
	;
RangeDeltaMonth
	:	'0'
	|	'-' [1-9] [0-9]*
	;
RangeADay
	:	[1-2] [0-9] [0-9] [0-9] [0-1] [0-9] [0-3] [0-9]
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
	:	[DWMY]
	;
EquiMark
	:	'X'
	;

WS
	:	[ \n\r\t] -> channel(HIDDEN)
	;
