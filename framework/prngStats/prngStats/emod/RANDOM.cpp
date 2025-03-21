/***************************************************************************************************

Copyright (c) 2018 Intellectual Ventures Property Holdings, LLC (IVPH) All rights reserved.

EMOD is licensed under the Creative Commons Attribution-Noncommercial-ShareAlike 4.0 License.
To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

***************************************************************************************************/

#include "stdafx.h"
#include "RANDOM.h"
#include <assert.h>
#include <math.h>

/**************************************************************************\
* NOTE: The caller must open a registry key before using the RANDOM class. *
*                                                                          *
* Example:                                                                 *
*                                                                          *
*   #include <registry.hxx>                                                *
*                                                                          *
*   REGISTRY reg("Software\\XYZ_Corp\\WhizCalc");                          *
*                                                                          *
* Here the name of the calling application is WhizCalc and the developer   *
* is XYZ Corp.  The state of the random sequence is stored there for later *
* runs.                                                                    *
\**************************************************************************/

int RANDOMBASE::cReg = 0;

double RANDOMBASE::cdf_random_num_precision = 0.01; // precision of random number obtained from a cumulative probability function
double RANDOMBASE::tan_pi_4 = 1.0;
double RANDOMBASE::pi = atan(tan_pi_4) * 4.0;

RANDOMBASE::RANDOMBASE() : last_ul(0)
{
    // HACK: we now need to allow multiple instances of an RNG to exist to support serialization properly
    //    assert(cReg==0);   // We permit only one registered sequence at a time!
    //    cReg = 1;
    bGauss = false;
    bSeq   = false; //reg.bReadULONG(LPCWSTR("Sequence"),&iSeq);
    bWrite = true;
}

RANDOMBASE::~RANDOMBASE()
{
    if (bWrite)
    {
        //reg.bWriteULONG(LPCWSTR("Sequence"),iSeq);
        cReg = 0;
    }
}

/******************************Public*Routine******************************\
* vShuffleFloats(pe,N)                                                     *
*                                                                          *
* A simple routine to shuffle a list of N floats.                          *
*                                                                          *
*  Sat 26-Mar-1994 13:59:00 -by- Charles Whitmer [chuckwh]                 *
* Wrote it.                                                                *
\**************************************************************************/

void RANDOMBASE::vShuffleFloats(float *pe, __USHORT N)
{
    float eTmp;
    __USHORT ii;

    while (N > 1)
    {
        ii = i(N--);
        eTmp = pe[N];
        pe[N] = pe[ii];
        pe[ii] = eTmp;
    }
}

/******************************Public*Routine******************************\
* eGauss()                                                                 *
*                                                                          *
* Returns a normal deviate for each call.  It generates two deviates at a  *
* time and caches one for the next call.                                   *
*                                                                          *
*  Sat 26-Mar-1994 14:10:12 -by- Charles Whitmer [chuckwh]                 *
* I actually wrote this back in 1982 for some physics stuff!               *
\**************************************************************************/

double RANDOMBASE::eGauss()
{
    if (bGauss)
    {
        bGauss = false;
        return eGauss_;
    }

    double rad, norm;
    double s, r1, r2;

    rad = -2.0 * log(ee());
    do
    {
        r1 = ee() - 0.5;
        r2 = ee() - 0.5;
        s = r1 * r1 + r2 * r2;
    }
    while (s > 0.25);
    norm = sqrt(rad / s);
    eGauss_ = r1 * norm;
    bGauss = true;
    return r2 * norm;
}

double RANDOMBASE::ee()
{
    union
    {
        double ee;
        struct
        {
            __ULONG Low;
            __ULONG High;
        };
    } ee_ul;

    __ULONG ll = ul();    // Choose a random 32 bits.

    ee_ul.ee = 1.0;
    ee_ul.High += (ll >> (DOUBLE_EXP + 1));
    ee_ul.Low = (ll << (31 - DOUBLE_EXP)) + (1 << (30 - DOUBLE_EXP));

    return ee_ul.ee - 1.0;
}

// Poisson() added by Philip Eckhoff, uses Gaussian approximation for ratetime>10
unsigned long long int RANDOMBASE::Poisson(double ratetime)
{
    if (ratetime <= 0)
    {
        return 0;
    }
    unsigned long long int events = 0;
    double Time = 0;
    double tempval;
    if (ratetime < 10)
    {
        while (Time < 1)
        {
            Time += -log(e()) / ratetime;
            if (Time < 1)
            {
                events++;
            }
        }
    }
    else
    {
        tempval = (eGauss() * sqrt(ratetime) + ratetime + .5);
        if (tempval < 0)
        {
            events = 0;
        }
        else
        {
            events = uint64_t(tempval);
        }
    }
    return events;
}
// Poisson_true added by Philip Eckhoff, actual Poisson, without approximation
unsigned long int RANDOMBASE::Poisson_true(double ratetime)
{
    if (ratetime <= 0)
    {
        return 0;
    }
    unsigned long int events = 0;
    double Time = 0;
    while (Time < 1)
    {
        Time += -log(e()) / ratetime;
        if (Time < 1)
        {
            events++;
        }
    }
    return events;
}

//  expdist() added by Philip Eckhoff
double RANDOMBASE::expdist(double rate) // rate = 1/mean
{
    if (rate == 0)
    {
        return 0;
    }
    else
    {
        return -log(e()) / rate;
    }
}

// binomial_approx() added by Philip Eckhoff
/*unsigned long int*/uint64_t RANDOMBASE::binomial_approx(uint64_t n, double p)
{
    /*long int*/int64_t tempval = 0;
    double mean = 0;
    if (n <= 0 || p <= 0)
    {
        tempval = 0;
    }
    else if (p >= 1)
    {
        tempval = n;
    }
    else
    {
        mean = n * p;
        if (n < 10)
        {
            for (int i = 0; i < n; i++)
            {
                if (e() < p)
                {
                    tempval++;
                }
            }
        }
        else
        {
            tempval = (int64_t)(eGauss() * sqrt(mean * (1.0 - p)) + mean + .5);
            if (tempval < 0)
            {
                tempval = 0;
            }
        }
    }

    if (tempval < 0)
    {
        tempval = 0;
    }
    if ((uint64_t)tempval > n)
    {
        tempval = n;
    }

    return uint64_t(tempval);
}

__ULONG RANDOM::ul()
{
    iSeq = 69069 * iSeq + 1;
    return iSeq;
}

const __ULONG c1[4] = {0xBAA96887L, 0x1E17D32CL, 0x03BCDC3CL, 0x0F33D1B2L};
const __ULONG c2[4] = {0x4B0F3B58L, 0xE874F0C3L, 0x6955C5A6L, 0x55A7CA46L};

#define HI(x) ((__ULONG) ((__USHORT *) &x)[1])
#define LO(x) ((__ULONG) ((__USHORT *) &x)[0])
#define XCHG(x) ((LO(x) << 16) | HI(x))

__ULONG PSEUDO_DES::ul()
{
    __ULONG kk[3], iA, iB;

    iA = iNum ^ c1[0];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[0] = iSeq ^ ((XCHG(iB) ^ c2[0]) + LO(iA) * HI(iA));

    iA = kk[0] ^ c1[1];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[1] = iNum ^ ((XCHG(iB) ^ c2[1]) + LO(iA) * HI(iA));

    iNum++;
    if (iNum == 0)
        iSeq++;

    iA = kk[1] ^ c1[2];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[2] = kk[0] ^ ((XCHG(iB) ^ c2[2]) + LO(iA) * HI(iA));

    iA = kk[2] ^ c1[3];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));

    last_ul = kk[1] ^ ((XCHG(iB) ^ c2[3]) + LO(iA) * HI(iA));

    return last_ul;
}

__ULONGLONG ullPDes(__ULONGLONG ullIn)
{
    __ULONGLONG_ ull;
    ull.Quad = ullIn;

    __ULONG kk[4], iA, iB;

    iA = ull.Low ^ c1[0];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[0] = ull.High ^ ((XCHG(iB) ^ c2[0]) + LO(iA) * HI(iA));

    iA = kk[0] ^ c1[1];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[1] = ull.Low ^ ((XCHG(iB) ^ c2[1]) + LO(iA) * HI(iA));

    iA = kk[1] ^ c1[2];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[2] = kk[0] ^ ((XCHG(iB) ^ c2[2]) + LO(iA) * HI(iA));

    iA = kk[2] ^ c1[3];
    iB = LO(iA) * LO(iA) + ~(HI(iA) * HI(iA));
    kk[3] = kk[1] ^ ((XCHG(iB) ^ c2[3]) + LO(iA) * HI(iA));

    ull.Low = kk[3];
    ull.High = kk[2];

    return ull.Quad;
}

__ULONG ulPDesTest[4][4] =
{
    { 1, 1, 0x604D1DCE, 0x509C0C23},
    { 1, 99, 0xD97F8571, 0xA66CB41A},
    {99, 1, 0x7822309D, 0x64300984},
    {99, 99, 0xD7F376F0, 0x59BA89EB}
};

void vTestPDes()
{
    __ULONGLONG_ ullA, ullB;

    for (int ii = 0; ii < 4; ii++)
    {
        ullA.High = ulPDesTest[ii][0];
        ullA.Low = ulPDesTest[ii][1];
        ullB.Quad = ullPDes(ullA.Quad);
        printf("(%03d:%03d) => %08X:%08X", ullA.High, ullA.Low, ullB.High, ullB.Low);
        if (ullB.High != ulPDesTest[ii][2] || ullB.Low != ulPDesTest[ii][3])
            printf("  Error!\n");
        else
            printf("\n");
    }
}

void vTestRandomFloats()
{
    __UINT ii;

    PSEUDO_DES randA(0);
    PSEUDO_DES randB(0);
    PSEUDO_DES randC(0);

    for (ii = 0; ii < 100; ii++)
    {
        __ULONG  ul = randA.ul();
        float  e  = randB.e();
        double ee = randC.ee();

        printf
        (
            "%08X %11.8f (%08X) %11.8f (%08X:%08X)\n",
            ul,
            e, ((__ULONG *)&e)[0],
            ee, ((__ULONG *)&ee)[1], ((__ULONG *)&ee)[0]
        );
    }
}



// M Behrend
// gamma-distributed random number
// shape constant k=2

double RANDOMBASE::rand_gamma(double mean)
{

    if (mean <= 0)
    {
        return 0;
    }
    double q = e(); // get a uniform random number

    double p1 = 0; // guess random variable interval
    double p2 = mean;
    double p_left; // temp variable
    double slope;
    double delta_p;
    double delta_cdf;

    do
    {
        delta_cdf = q - gamma_cdf(p2, mean);
        slope = (gamma_cdf(p2, mean) - gamma_cdf(p1, mean)) / (p2 - p1);
        delta_p = delta_cdf / slope;

        if (delta_p > 0)
        {
            p1 = p2;
            p2 = p2 + delta_p;
        }
        else
        {
            p2 = p2 + delta_p;
            if (p2 < p1)
            {
                p_left = p2;
                p2 = p1;
                p1 = p_left;
            }
        }
    }
    while (fabs(delta_cdf) > cdf_random_num_precision);

    return p2;
}

double RANDOMBASE::gamma_cdf(double x, double mean)
{
    double theta = mean / 2;
    double cdf = 1 - (x / theta + 1) * exp(-x / theta);

    if (x < 0)
    {
        cdf = 0;
    }
    return cdf;
}

double RANDOMBASE::get_cdf_random_num_precision()
{
    return cdf_random_num_precision;
}

double RANDOMBASE::get_pi()
{
    return pi;
}
