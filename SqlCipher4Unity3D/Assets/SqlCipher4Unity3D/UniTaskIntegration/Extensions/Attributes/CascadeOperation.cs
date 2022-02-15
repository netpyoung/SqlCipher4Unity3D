namespace SqlCipher4Unity3D.UniTaskIntegration.Extensions.Attributes
{
    using System;

    [Flags]
    public enum CascadeOperation {
        None                        = 0,
        CascadeRead                 = 1 << 1,
        CascadeInsert               = 1 << 2,
        CascadeDelete               = 1 << 3,
        All                         = CascadeRead | CascadeInsert | CascadeDelete
    }
}