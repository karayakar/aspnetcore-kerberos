﻿using System;
using System.Linq;
using Microsoft.AspNetCore.Authentication.GssKerberos.Native;
using Microsoft.AspNetCore.Authentication.GssKerberos.Sspi;
using SecurityBufferType = Microsoft.AspNetCore.Authentication.GssKerberos.Sspi.SecurityBufferType;

// ReSharper disable InconsistentNaming

namespace Microsoft.AspNetCore.Authentication.GssKerberos.Gss
{
    public class SspiAcceptor : IAcceptor
    {
        private SecurityHandle _credentials;
        private SecurityHandle _context;
        private long _expiryTime;

        private const uint HTTP_SECURITY_ATTRIBUTES =
            SspiInterop.ISC_REQ_REPLAY_DETECT   | 
            SspiInterop.ISC_REQ_SEQUENCE_DETECT | 
            SspiInterop.ISC_REQ_CONNECTION;

        public bool IsEstablished { get; private set; }

        /// <summary>
        /// The UPN of the context initiator
        /// </summary>
        public string Principal { get; private set; }

        /// <summary>
        /// The final negotiated flags
        /// </summary>
        public uint Flags { get; private set; }

        /// <summary>
        /// The time the token expires
        /// </summary>
        public uint Expiry { get; private set; }


        public SspiAcceptor(SspiCredentials credentials)
        {
            _credentials = credentials.Credentials;
        }

        public byte[] Accept(byte[] token)
        {
            // TODO: A SecBufferDesc builder would be nice
            var incomingToken = new SecurityBufferDescription
            {
                Version = 0,
                Buffers = new[]
                {
                    new SecurityBuffer
                    {
                        Buffer = token,
                        BufferType = SecurityBufferType.SECBUFFER_TOKEN
                    }
                }
            };

            var outgoingToken = new SecurityBufferDescription
            {
                Version = 0,
                Buffers = new[]
                {
                    new SecurityBuffer
                    {
                        Buffer = new Byte[32000],
                        BufferType = SecurityBufferType.SECBUFFER_TOKEN
                    }
                }
            };

            var result = SspiInterop.AcceptSecurityContext(
                ref _credentials,
                IntPtr.Zero, 
                incomingToken,
                HTTP_SECURITY_ATTRIBUTES,
                SspiInterop.SECURITY_NATIVE_DREP,
                ref _context,
                outgoingToken,
                out var attributes,
                out var expiry);

            if (result != SspiInterop.SEC_I_CONTINUE_NEEDED)
            {
                throw new Exception("The SSPI Negotiate package was unable to accept the supplied authentication token");
            }

            CompleteContext(outgoingToken, attributes, expiry);

            return outgoingToken.Buffers
                .FirstOrDefault(buffer => buffer.BufferType == SecurityBufferType.SECBUFFER_TOKEN)
                .Buffer;
        }

        private void CompleteContext(SecurityBufferDescription description, uint attributes, long expiry)
        {
            IsEstablished = true;
        }

        public void Dispose()
        {
        }
    }
}