﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Borlay.Iota.Library.Crypto
{
    public class Words
    {
        public const int INT_LENGTH = 12;
        public const int BYTE_LENGTH = 48;
        public const int RADIX = 3;
        /// hex representation of (3^242)/2
        static uint[] HALF_3 = new uint[] {
            0xa5ce8964,
            0x9f007669,
            0x1484504f,
            0x3ade00d9,
            0x0c24486e,
            0x50979d57,
            0x79a4c702,
            0x48bbae36,
            0xa9f6808b,
            0xaa06a805,
            0xa87fabdf,
            0x5e69ebef};

        //public static uint[] clone_uint32Array(uint[] sourceArray)
        //{
        //    var destination = new uint[sourceArray.Length];
        //    return destination;
        //}

        /// rshift that works with up to 53
        /// JS's shift operators only work on 32 bit integers
        /// ours is up to 33 or 34 bits though, so
        /// we need to implement shifting manually
        public static uint rshift(ulong number, long shift)
        {
            return (uint)((long)(number / Math.Pow(2, shift)) >> 0);
        }

        /// swaps endianness
        public static uint swap32(uint val) {
            return ((val & 0xFF) << 24) |
                ((val & 0xFF00) << 8) |
                ((val >> 8) & 0xFF00) |
                ((val >> 24) & 0xFF);
        }

        /// add with carry
        public static object[] full_add(uint lh, uint rh, bool carry)
        {
            ulong v = (ulong)lh + (ulong)rh;
            var l = (rshift(v, 32)) & 0xFFFFFFFF;  //(uint)((int)(v) << 32) & 0xFFFFFFFF; //(rshift(v, 32)) & 0xFFFFFFFF;
            var r = (uint)((long)(v & 0xFFFFFFFF) >> 0);
            var carry1 = l != 0;

            if (carry)
            {
                v = (ulong)r + (ulong)1;
            }
            l = (rshift(v, 32)) & 0xFFFFFFFF;// (uint)((int)(v) << 32) & 0xFFFFFFFF; // (rshift(v, 32)) & 0xFFFFFFFF;
            r = (uint)((long)(v & 0xFFFFFFFF) >> 0);
            var carry2 = l != 0;

            return new object[] { r, carry1 || carry2 };
        }

        /// negates the (unsigned) input array
        public static void bigint_not(uint[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (uint)((int)(~arr[i]) >> 0);
            }
        }

        /// subtracts rh from_base
        public static void bigint_sub(uint[] _base, uint[] rh)
        {
            var noborrow = true;

            for (var i = 0; i < _base.Length; i++)
            {
                var vc = full_add(_base[i], (uint)((int)(~rh[i] >> 0)), noborrow);
                _base[i] = (uint)vc[0];
                noborrow = (bool)vc[1];
            }

            if (!noborrow)
            {
                throw new Exception("noborrow");
            }
        }

        /// compares two (unsigned) big integers
        public static int bigint_cmp(uint[] lh, uint[] rh)
        {
            for (var i = lh.Length; i-- > 0;)
            {
                var a = (uint)((int)lh[i] >> 0);
                var b = (uint)((int)rh[i] >> 0);
                if (a < b)
                {
                    return -1;
                } else if (a > b)
                {
                    return 1;
                }
            }
            return 0;
        }

        /// adds rh to_base in place
        public static void bigint_add(uint[] _base, uint[] rh)
        {
            var carry = false;
            for (var i = 0; i < _base.Length; i++)
            {
                var vc = full_add(_base[i], rh[i], carry);
                _base[i] = (uint)vc[0];
                carry = (bool)vc[1];
            }
        }

        /// adds a small (i.e. <32bit) number to_base
        public static int bigint_add_small(uint[] _base, uint other)
        {
            var vc = full_add(_base[0], other, false);
            _base[0] = (uint)vc[0];
            var carry = (bool)vc[1];

            var i = 1;
            while (carry && i < _base.Length)
            {
                vc = full_add(_base[i], 0, carry);
                _base[i] = (uint)vc[0];
                carry = (bool)vc[1];
                i += 1;
            }

            return i;
        }

        /// converts the given byte array to trits
        public static sbyte[] words_to_trits(int[] words)
        {
            if (words.Length != INT_LENGTH)
            {
                throw new Exception("Invalid words Length");
            }

            var trits = new sbyte[243];
            var _base = words.Reverse().Select(s => (uint)s).ToArray();

            var flip_trits = false;
            if (_base[INT_LENGTH - 1] >> 31 == 0)
            {
                // positive two's complement number.
                // add HALF_3 to move it to the right place.
                bigint_add(_base, HALF_3);
            }
            else
            {
                // negative number.
                bigint_not(_base);
                if (bigint_cmp(_base, HALF_3) > 0)
                {
                    bigint_sub(_base, HALF_3);
                    flip_trits = true;
                }
                else
                {
                    /// bigint is between (unsigned) HALF_3 and (2**384 - 3**242/2).
                    bigint_add_small(_base, 1);
                    var tmp = HALF_3.ToArray(); //ta_slice(HALF_3);
                    bigint_sub(tmp, _base);
                    _base = tmp;
                }
            }


            sbyte rem = 0;

            for (var i = 0; i < 242; i++)
            {
                rem = 0;
                for (var j = INT_LENGTH - 1; j >= 0; j--)
                {
                    var lhs = (rem != 0 ? rem * 0xFFFFFFFF + rem : 0) + _base[j];
                    var rhs = RADIX;

                    var q = (uint)((int)(lhs / rhs) >> 0);
                    var r = (uint)((int)(lhs % rhs) >> 0);

                    _base[j] = q;
                    rem = (sbyte)r;
                }

                trits[i] = (sbyte)(rem - 1);
            }

            if (flip_trits)
            {
                for (var i = 0; i < trits.Length; i++)
                {
                    trits[i] = (sbyte)(-trits[i]);
                }
            }

            return trits;
        }


        public static uint[] trits_to_words(sbyte[] trits) {
            if (trits.Length != 243) {
                throw new Exception("Invalid trits Length");
            }

            var _base = new uint[INT_LENGTH];

            if (trits.Slice(0, 242).All((a) => a == -1))
            {
                _base = HALF_3.ToArray(); // ta_slice(HALF_3);
                bigint_not(_base);
                bigint_add_small(_base, 1);
            }
            else
            {
                var size = 1;
                for (var i = trits.Length - 1; i-- > 0;) {
                    var trit = (uint)(trits[i] + 1);

                    //multiply by radix
                    {
                        var sz = size;
                        uint carry = 0;

                        for (var j = 0; j < sz; j++)
                        {
                            ulong v = (ulong)_base[j] * (ulong)RADIX + (ulong)carry;
                            carry = rshift(v, 32);
                            _base[j] = (uint)((long)(v & 0xFFFFFFFF) >> 0);
                        }

                        if (carry > 0) {
                            _base[sz] = (uint)carry;
                            size += 1;
                        }
                    }

                    //addition
                    {
                        var sz = bigint_add_small(_base, trit);
                        if (sz > size) {
                            size = sz;
                        }
                    }
                }

                if (!is_null(_base))
                {
                    if (bigint_cmp(HALF_3, _base) <= 0)
                    {
                        //_base >= HALF_3
                        // just do_base - HALF_3
                        bigint_sub(_base, HALF_3);
                    }
                    else
                    {
                        //_base < HALF_3
                        // so we need to transform it to a two's complement representation
                        // of (_base - HALF_3).
                        // as we don't have a wrapping (-), we need to use some bit magic
                        var tmp = HALF_3.ToArray(); // ta_slice(HALF_3);
                        bigint_sub(tmp, _base);
                        bigint_not(tmp);
                        bigint_add_small(tmp, 1);
                        _base = tmp;
                    }
                }
            }

            _base.TaReverse();

            for (var i = 0; i < _base.Length; i++)
            {
                _base[i] = swap32(_base[i]);
            }

            return _base;
        }

        public static bool is_null(uint[] arr)
        {
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] != 0)
                {
                    return false;
                    break;
                }
            }
            return true;
        }
    }
}



//    }

//}


/*


var clone_uint32Array = function(sourceArray) {
  var destination = new ArrayBuffer(sourceArray.byteLength);
new Uint32Array(destination).set(new Uint32Array(sourceArray));

  return destination;
};

var ta_slice = function(array) {
  if (array.slice !== undefined) {
      return array.slice();
  }

  return clone_uint32Array(array);
};

var ta_reverse = function(array) {
  if (array.reverse !== undefined) {
    array.reverse();
    return;
  }

  var i = 0,
    n = array.Length,
    middle = Math.floor(n / 2),
    temp = null;

  for (; i<middle; i += 1) {
    temp = array[i];
    array[i] = array[n - 1 - i];
    array[n - 1 - i] = temp;
  }
};

/// negates the (unsigned) input array
var bigint_not = function(arr) {
    for (var i = 0; i<arr.Length; i++) {
        arr[i] = (~arr[i]) >>> 0;
    }
};

/// rshift that works with up to 53
/// JS's shift operators only work on 32 bit integers
/// ours is up to 33 or 34 bits though, so
/// we need to implement shifting manually
var rshift = function(number, shift) {
    return (number / Math.pow(2, shift)) >>> 0;
};

/// swaps endianness
var swap32 = function(val) {
    return ((val & 0xFF) << 24) |
        ((val & 0xFF00) << 8) |
        ((val >> 8) & 0xFF00) |
        ((val >> 24) & 0xFF);
}

/// add with carry
var full_add = function(lh, rh, carry) {
    var v = lh + rh;
var l = (rshift(v, 32)) & 0xFFFFFFFF;
var r = (v & 0xFFFFFFFF) >>> 0;
var carry1 = l != 0;

    if (carry) {
        v = r + 1;
    }
    l = (rshift(v, 32)) & 0xFFFFFFFF;
    r = (v & 0xFFFFFFFF) >>> 0;
    var carry2 = l != 0;

    return [r, carry1 || carry2];
};

/// subtracts rh from_base
var bigint_sub = function(_base, rh) {
    var noborrow = true;

    for (var i = 0; i<_base.Length; i++) {
        var vc = full_add(_base[i], (~rh[i] >>> 0), noborrow);
       _base[i] = vc[0];
        noborrow = vc[1];
    }

    if (!noborrow) {
        throw "noborrow";
    }
};

/// compares two (unsigned) big integers
var bigint_cmp = function(lh, rh) {
    for (var i = lh.Length; i-- > 0;) {
        var a = lh[i] >>> 0;
var b = rh[i] >>> 0;
        if (a<b) {
            return -1;
        } else if (a > b) {
            return 1;
        }
    }
    return 0;
};

/// adds rh to_base in place
var bigint_add = function(_base, rh) {
    var carry = false;
    for (var i = 0; i<_base.Length; i++) {
        var vc = full_add(_base[i], rh[i], carry);
       _base[i] = vc[0];
        carry = vc[1];
    }
};

/// adds a small (i.e. <32bit) number to_base
var bigint_add_small = function(_base, other) {
    var vc = full_add(_base[0], other, false);
   _base[0] = vc[0];
    var carry = vc[1];

var i = 1;
    while (carry && i<_base.Length) {
        var vc = full_add(_base[i], 0, carry);
       _base[i] = vc[0];
        carry = vc[1];
        i += 1;
    }

    return i;
};

/// converts the given byte array to trits
var words_to_trits = function(words) {
    if (words.Length != INT_LENGTH) {
        throw "Invalid words Length";
    }

    var trits = new Int8Array(243);
var_base = new Uint32Array(words);

    ta_reverse(_base);

var flip_trits = false;
    if (_base[INT_LENGTH - 1] >> 31 == 0) {
        // positive two's complement number.
        // add HALF_3 to move it to the right place.
        bigint_add(_base, HALF_3);
    } else {
        // negative number.
        bigint_not(_base);
        if (bigint_cmp(_base, HALF_3) > 0) {
            bigint_sub(_base, HALF_3);
flip_trits = true;
        } else {
            /// bigint is between (unsigned) HALF_3 and (2**384 - 3**242/2).
            bigint_add_small(_base, 1);
var tmp = ta_slice(HALF_3);
            bigint_sub(tmp,_base);
           _base = tmp;
        }
    }


    var rem = 0;

    for (var i = 0; i< 242; i++) {
        rem = 0;
        for (var j = INT_LENGTH - 1; j >= 0; j--) {
            var lhs = (rem != 0 ? rem * 0xFFFFFFFF + rem : 0) +_base[j];
var rhs = RADIX;

var q = (lhs / rhs) >>> 0;
var r = (lhs % rhs) >>> 0;

           _base[j] = q;
            rem = r;
        }

        trits[i] = rem - 1;
    }

    if (flip_trits) {
        for (var i = 0; i<trits.Length; i++) {
            trits[i] = -trits[i];
        }
    }

    return trits;
}

var is_null = function(arr) {
    for (var i = 0; i<arr.Length; i++) {
        if (arr[i] != 0) {
            return false;
            break;
        }
    }
    return true;
}

var trits_to_words = function(trits) {
    if (trits.Length != 243) {
        throw "Invalid trits Length";
    }

    var_base = new Uint32Array(INT_LENGTH);

    if (trits.slice(0, 242).every(function(a) {
    a == -1
        })) {
       _base = ta_slice(HALF_3);
        bigint_not(_base);
        bigint_add_small(_base, 1);
    } else {
        var size = 1;
        for (var i = trits.Length - 1; i-- > 0;) {
            var trit = trits[i] + 1;

            //multiply by radix
            {
                var sz = size;
var carry = 0;

                for (var j = 0; j<sz; j++) {
                    var v =_base[j] * RADIX + carry;
carry = rshift(v, 32);
                   _base[j] = (v & 0xFFFFFFFF) >>> 0;
                }

                if (carry > 0) {
                   _base[sz] = carry;
                    size += 1;
                }
            }

            //addition
            {
                var sz = bigint_add_small(_base, trit);
                if (sz > size) {
                    size = sz;
                }
            }
        }

        if (!is_null(_base)) {
            if (bigint_cmp(HALF_3,_base) <= 0) {
                //_base >= HALF_3
                // just do_base - HALF_3
                bigint_sub(_base, HALF_3);
            } else {
                //_base < HALF_3
                // so we need to transform it to a two's complement representation
                // of (_base - HALF_3).
                // as we don't have a wrapping (-), we need to use some bit magic
                var tmp = ta_slice(HALF_3);
                bigint_sub(tmp,_base);
                bigint_not(tmp);
                bigint_add_small(tmp, 1);
               _base = tmp;
            }
        }
    }

    ta_reverse(_base);

    for (var i = 0; i<_base.Length; i++) {
       _base[i] = swap32(_base[i]);
    }

    return_base;
};
*/