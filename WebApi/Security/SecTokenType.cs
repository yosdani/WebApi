﻿namespace WebApi.Security
{
    [Flags]
    public enum SecTokenType
    {
        None = 1,
        Public = 2,
        Email = 4
    }
}
