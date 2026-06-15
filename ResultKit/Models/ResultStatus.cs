using System;
using System.Collections.Generic;
using System.Text;

namespace ResultKit.Models
{
    /// <summary>
    /// Represents the semantic status of a result, mapping naturally to HTTP status codes.
    /// </summary>
    public enum ResultStatus
    {
        /// <summary>200 — Operation succeeded.</summary>
        Ok,

        /// <summary>201 — Resource was successfully created.</summary>
        Created,

        /// <summary>204 — Operation succeeded with no content to return.</summary>
        NoContent,

        /// <summary>400 — The request was malformed or contained invalid data.</summary>
        Invalid,

        /// <summary>401 — Authentication is required or has failed.</summary>
        Unauthorized,

        /// <summary>403 — The caller is authenticated but not permitted.</summary>
        Forbidden,

        /// <summary>404 — The requested resource does not exist.</summary>
        NotFound,

        /// <summary>409 — A conflict with the current state of the resource.</summary>
        Conflict,

        /// <summary>422 — Unprocessable entity; semantic validation failed.</summary>
        UnprocessableEntity,

        /// <summary>500 — An unexpected internal error occurred.</summary>
        Error
    }

}
