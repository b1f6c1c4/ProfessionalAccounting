/* Copyright (C) 2020 b1f6c1c4
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

using System.Collections.Generic;

namespace AccountingServer.Http
{
    internal static class ResponseCodes
    {
        private static readonly Dictionary<int, string> Codes
            = new Dictionary<int, string>
                {
                    { 100, "Continue" },
                    { 101, "Switching Protocols" },
                    { 200, "OK" },
                    { 201, "Created" },
                    { 202, "Accepted" },
                    { 203, "Non-Authoritative Information" },
                    { 204, "No Content" },
                    { 205, "Reset Content" },
                    { 206, "Partial Content" },
                    { 300, "Multiple Choices" },
                    { 301, "Moved Permanently" },
                    { 302, "Found" },
                    { 303, "See Other" },
                    { 304, "Not Modified" },
                    { 305, "Use Proxy" },
                    { 307, "Temporary Redirect" },
                    { 400, "Bad Request" },
                    { 401, "Unauthorized" },
                    { 402, "Payment Required" },
                    { 403, "Forbidden" },
                    { 404, "Not Found" },
                    { 405, "Method Not Allowed" },
                    { 406, "Not Acceptable" },
                    { 407, "Proxy Authentication Required" },
                    { 408, "Request Timeout" },
                    { 409, "Conflict" },
                    { 410, "Gone" },
                    { 411, "Length Required" },
                    { 412, "Precondition Failed" },
                    { 413, "Request Entity Too Large" },
                    { 414, "Request-URI Too Long" },
                    { 415, "Unsupported Media Type" },
                    { 416, "Requested Range Not Satisfiable" },
                    { 417, "Expectation Failed" },
                    { 500, "Internal Server Error" },
                    { 501, "Not Implemented" },
                    { 502, "Bad Gateway" },
                    { 503, "Service Unavailable" },
                    { 504, "Gateway Timeout" },
                    { 505, "HTTP Version Not Supported" },
                };

        public static string Get(int code) => Codes[code];
    }
}
