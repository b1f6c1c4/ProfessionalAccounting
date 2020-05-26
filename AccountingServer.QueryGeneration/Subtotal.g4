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
