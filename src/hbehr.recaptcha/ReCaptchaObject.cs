﻿/*
 * The MIT License (MIT)
 * 
 * Copyright (c) 2015 - 2016 Henrique B. Behr
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web;
using hbehr.recaptcha.Exceptions;
using hbehr.recaptcha.Internazionalization;
using hbehr.recaptcha.WebInterface;

namespace hbehr.recaptcha
{
    internal class ReCaptchaObject
    {
        private string _captchaDiv, _secretKey, _language;
        private bool _configured;
        
        internal ReCaptchaObject()
        {
            try
            {
                // Auto .config configuration
                var reader = new AppSettingsReader();
                string secretKey = reader.GetValue("recaptcha-secret-key", typeof(string)).ToString();
                string publicKey = reader.GetValue("recaptcha-public-key", typeof(string)).ToString();

                Initialize(publicKey, secretKey);
                try
                {
                    _language = reader.GetValue("recaptcha-language-key", typeof(string)).ToString();
                }
                catch
                {
                    // No language on .config
                }
            }
            catch
            {
                // No configuration on .config
            }
        }

        internal ReCaptchaObject(string publicKey, string secretKey, ReCaptchaLanguage? defaultLanguage = null)
        {
            Initialize(publicKey, secretKey, defaultLanguage);
        }

        private void Initialize(string publicKey, string secretKey, ReCaptchaLanguage? defaultLanguage = null)
        {
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentNullException("publicKey");
            }
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new ArgumentNullException("secretKey");
            }
            if (defaultLanguage.HasValue)
            {
                _language = defaultLanguage.Value.GetLanguage();
            }

            _configured = true;
            _secretKey = secretKey;
            _captchaDiv = string.Format("<div class='g-recaptcha' data-sitekey='{0}'></div><script src='https://www.google.com/recaptcha/api.js{{0}}'></script>", publicKey);
        }

        private string GetHlCode(ReCaptchaLanguage? language)
        {
            string strLang = language.HasValue ? language.Value.GetLanguage() : _language;
            return string.IsNullOrWhiteSpace(strLang) ? "" : "?hl=" + strLang;
        }

        private void CheckIfIamConfigured()
        {
            if (_configured) { return; }
            throw new ReCaptchaException("ReCaptcha is not configured. Get your site and secret keys from google. And call function ReCaptcha.Configure(publicKey, secretKey), or add the keys to the .config file <add key='recaptcha-public-key' value='...' /><add key='recaptcha-site-key' value='...'/>");
        }

        internal IHtmlString GetCaptcha(ReCaptchaLanguage? language)
        {
            CheckIfIamConfigured();
            return new HtmlString(string.Format(_captchaDiv, GetHlCode(language)));
        }

        internal bool ValidateResponse(IReChaptaWebInterface webInterface, string response)
        {
            CheckIfIamConfigured();
            var answer = webInterface.PostUserAnswer(response, _secretKey);
            TreatReCaptchaError(answer);
            return answer.Success;
        }

#if !NET40
        internal async Task<bool> ValidateResponseAsync(IReChaptaWebInterfaceAsync webInterface, string response)
        {
            CheckIfIamConfigured();
            var answer = await webInterface.PostUserAnswerAsync(response, _secretKey);
            TreatReCaptchaError(answer);
            return answer.Success;
        }
#endif

        private static void TreatReCaptchaError(ReCaptchaJsonResponse answer)
        {
            var error = new ReCaptchaError(answer.ErrorCodes);

            if (error.InvalidInputSecret)
            {
                throw new ReCaptchaException("Invalid ReCaptcha Secret Key !");
            }
            if (error.InvalidInputResponse)
            {
                throw new ReCaptchaException("Invalid Input Response, make sure you are passing correctly the user answer from the Captcha.");
            }
        }
    }
}