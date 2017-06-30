// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authentication.Internal;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Contains the options used by the <see cref="RemoteAuthenticationHandler{T}"/>.
    /// </summary>
    public class RemoteAuthenticationOptions : AuthenticationSchemeOptions
    {
        internal const string CorrelationPrefix = ".AspNetCore.Correlation.";

        private CookieBuilder _correlationIdCookieBuilder;

        /// <summary>
        /// Initializes a new <see cref="RemoteAuthenticationOptions"/>.
        /// </summary>
        public RemoteAuthenticationOptions()
        {
            _correlationIdCookieBuilder = new CorrelationIdCookieBuilder(this)
            {
                Name = CorrelationPrefix,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
            };
        }

        /// <summary>
        /// Check that the options are valid.  Should throw an exception if things are not ok.
        /// </summary>
        public override void Validate()
        {
            base.Validate();
            if (CallbackPath == null || !CallbackPath.HasValue)
            {
                throw new ArgumentException(Resources.FormatException_OptionMustBeProvided(nameof(CallbackPath)), nameof(CallbackPath));
            }
        }

        /// <summary>
        /// Gets or sets timeout value in milliseconds for back channel communications with the remote identity provider.
        /// </summary>
        /// <value>
        /// The back channel timeout.
        /// </value>
        public TimeSpan BackchannelTimeout { get; set; } = TimeSpan.FromSeconds(60);

        /// <summary>
        /// The HttpMessageHandler used to communicate with remote identity provider.
        /// This cannot be set at the same time as BackchannelCertificateValidator unless the value 
        /// can be downcast to a WebRequestHandler.
        /// </summary>
        public HttpMessageHandler BackchannelHttpHandler { get; set; }

        /// <summary>
        /// Used to communicate with the remote identity provider.
        /// </summary>
        public HttpClient Backchannel { get; set; }

        /// <summary>
        /// Gets or sets the type used to secure data.
        /// </summary>
        public IDataProtectionProvider DataProtectionProvider { get; set; }

        /// <summary>
        /// The request path within the application's base path where the user-agent will be returned.
        /// The middleware will process this request when it arrives.
        /// </summary>
        public PathString CallbackPath { get; set; }

        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the middleware
        /// responsible of persisting user's identity after a successful authentication.
        /// This value typically corresponds to a cookie middleware registered in the Startup class.
        /// When omitted, <see cref="AuthenticationOptions.DefaultSignInScheme"/> is used as a fallback value.
        /// </summary>
        public string SignInScheme { get; set; }

        /// <summary>
        /// Gets or sets the time limit for completing the authentication flow (15 minutes by default).
        /// </summary>
        public TimeSpan RemoteAuthenticationTimeout { get; set; } = TimeSpan.FromMinutes(15);

        public new RemoteAuthenticationEvents Events
        {
            get => (RemoteAuthenticationEvents)base.Events;
            set => base.Events = value;
        }

        /// <summary>
        /// Defines whether access and refresh tokens should be stored in the
        /// <see cref="Http.Authentication.AuthenticationProperties"/> after a successful authorization.
        /// This property is set to <c>false</c> by default to reduce
        /// the size of the final authentication cookie.
        /// </summary>
        public bool SaveTokens { get; set; }

        /// <summary>
        /// Determines the settings used to create the correlation id cookie before the
        /// cookie gets added to the response.
        /// </summary>
        public CookieBuilder CorrelationIdCookie
        {
            get => _correlationIdCookieBuilder;
            set => _correlationIdCookieBuilder = value ?? throw new ArgumentNullException(nameof(value));
        }

        private class CorrelationIdCookieBuilder : RequestPathCookieBuilder
        {
            private readonly RemoteAuthenticationOptions _options;

            public CorrelationIdCookieBuilder(RemoteAuthenticationOptions remoteAuthenticationOptions)
            {
                _options = remoteAuthenticationOptions;
            }

            protected override string AdditionalPath => _options.CallbackPath;

            public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
            {
                var cookieOptions = base.Build(context, expiresFrom);

                if (!Expiration.HasValue || !cookieOptions.Expires.HasValue)
                {
                    cookieOptions.Expires = expiresFrom.Add(_options.RemoteAuthenticationTimeout);
                }

                return cookieOptions;
            }
        }
    }
}
