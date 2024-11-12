﻿// GPU Instancer Pro
// Copyright (c) GurBu Technologies

#ifndef __matrix_hlsl_
#define __matrix_hlsl_

// Sets the rotation of a translation matrix "m" by the quaternion value "q".
float4x4 SetMatrixRotationWithQuaternion(float4x4 m, float4 q)
{
	// See e.g. http://www.geometrictools.com/Documentation/LinearAlgebraicQuaternions.pdf .
    
    float x = q.x;
    float y = q.y;
    float z = q.z;
    float w = q.w;
    m[0][0] = 1 - 2 * (y * y + z * z);
    m[0][1] = 2 * (x * y - z * w);
    m[0][2] = 2 * (x * z + y * w);
    m[1][0] = 2 * (x * y + z * w);
    m[1][1] = 1 - 2 * (x * x + z * z);
    m[1][2] = 2 * (y * z - x * w);
    m[2][0] = 2 * (x * z - y * w);
    m[2][1] = 2 * (y * z + x * w);
    m[2][2] = 1 - 2 * (x * x + y * y);

    return m;
}

/// https://www.3dgep.com/3d-math-primer-for-game-programmers-matrices/
/// n : axis
/// a : angle in radians
/// A 3D rotation about an arbitrary axis n by an angle of a
float3x3 MatrixRotate3x3(float3 n, float a)
{
    float cosa = cos(a);
    float sina = sin(a);
    
    return float3x3(
	n.x * n.x * (1 - cosa) + cosa,
	n.x * n.y * (1 - cosa) - n.z * sina,
	n.x * n.z * (1 - cosa) + n.y * sina,
    n.x * n.y * (1 - cosa) + n.z * sina,
    n.y * n.y * (1 - cosa) + cosa,
    n.y * n.z * (1 - cosa) - n.x * sina,
    n.x * n.z * (1 - cosa) - n.y * sina,
    n.y * n.z * (1 - cosa) + n.x * sina,
    n.z * n.z * (1 - cosa) + cosa
	);
}

/// https://www.3dgep.com/3d-math-primer-for-game-programmers-matrices/
/// n : axis
/// a : angle in radians
/// A 3D rotation about an arbitrary axis n by an angle of a
float4x4 MatrixRotate(float3 n, float a)
{
    float cosa = cos(a);
    float sina = sin(a);
    
    return float4x4(
	n.x * n.x * (1 - cosa) + cosa,
	n.x * n.y * (1 - cosa) - n.z * sina,
	n.x * n.z * (1 - cosa) + n.y * sina,
	0,
    n.x * n.y * (1 - cosa) + n.z * sina,
    n.y * n.y * (1 - cosa) + cosa,
    n.y * n.z * (1 - cosa) - n.x * sina,
	0,
    n.x * n.z * (1 - cosa) - n.y * sina,
    n.y * n.z * (1 - cosa) + n.x * sina,
    n.z * n.z * (1 - cosa) + cosa,
	0,
	0, 0, 0, 1
	);
}

// m: matrix to scale
// angle: angles for x,y,z
// returns rotation matrix
float4x4 MatrixRotateXYZ(float3 angle)
{
	// rotate at x axis
    float4x4 mX = MatrixRotate(float3(1, 0, 0), radians(angle.x));
	// rotate at y axis
    float4x4 mY = MatrixRotate(float3(0, 1, 0), radians(angle.y));
	// rotate at z axis
    float4x4 mYmX = mul(mY, mX);
    float4x4 mZ = MatrixRotate(float3(mYmX[0][2], mYmX[1][2], mYmX[2][2]), radians(angle.z));
	// return result
    return mul(mul(mZ, mY), mX);
}


// Scales the matrix "m" by "scale" scales for x,y,z.
float4x4 SetScaleOfMatrix(float4x4 m, float3 scale)
{
    m._m00_m10_m20_m30 = m._m00_m10_m20_m30 * scale.x / length(m._m00_m10_m20_m30);
    m._m01_m11_m21_m31 = m._m01_m11_m21_m31 * scale.y / length(m._m01_m11_m21_m31);
    m._m02_m12_m22_m32 = m._m02_m12_m22_m32 * scale.z / length(m._m02_m12_m22_m32);

    return m;
}

// Scales the matrix "m" by "scale" scales for x,y,z.
float4x4 SetScaleOfMatrixPercentage(float4x4 m, float3 scale)
{
    m._m00_m10_m20_m30 = m._m00_m10_m20_m30 * scale.x;
    m._m01_m11_m21_m31 = m._m01_m11_m21_m31 * scale.y;
    m._m02_m12_m22_m32 = m._m02_m12_m22_m32 * scale.z;

    return m;
}

// Generates a Matrix4x4 from position, rotation and scale.
float4x4 TRS(in float3 position, in float4x4 rotationMatrix, in float3 scale)
{
	// start with rotation matrix and set scale
    float4x4 result = SetScaleOfMatrix(rotationMatrix, scale);
	// set position
    result._m03_m13_m23 = position;
	// return result
    return result;
}

// Returns a quaternion that represents a rotation of "angle" degrees along "axis". 
float4 AngleAxis(float3 axis, float angle)
{
    // Calculate the sin( theta / 2) once for optimization
    double factor = sin(angle / 2.0);

    // Calculate the x, y and z of the quaternion
    double x = axis.x * factor;
    double y = axis.y * factor;
    double z = axis.z * factor;

    // Calculate the w value by cos( theta / 2 )
    double w = cos(angle / 2.0);

    return normalize(float4(x, y, z, w));
}

// Quaternion Multiplication.
float4 QuatMul(float4 q1, float4 q)
{
    float w_val = q1.w * q.w - q1.x * q.x - q1.y * q.y - q1.z * q.z;
    float x_val = q1.w * q.x + q1.x * q.w + q1.y * q.z - q1.z * q.y;
    float y_val = q1.w * q.y + q1.y * q.w + q1.z * q.x - q1.x * q.z;
    float z_val = q1.w * q.z + q1.z * q.w + q1.x * q.y - q1.y * q.x;
  
    q1.w = w_val;
    q1.x = x_val;
    q1.y = y_val;
    q1.z = z_val;

   return q1;
}

// Returns a quaternion that represents a rotation from "from" to "to" vectors.
float4 FromToRotation(float3 from, float3 to)
{
    float4 q;
    float3 a = cross(from, to);
    q.xyz = a;
    q.w = sqrt((length(to) * length(to))) + dot(from, to);
    q = normalize(q);

    return q;
}

// Returns the scale from transform matrix
float3 GetScale(float4x4 mat)
{
    return float3(length(mat._11_12_13), length(mat._21_22_23), length(mat._31_32_33));
}

float4x4 GetInverseTransformMatrix(float4x4 o2w)
{
	// inverse transform matrix
	// taken from richardkettlewell's post on
	// https://forum.unity3d.com/threads/drawmeshinstancedindirect-example-comments-and-questions.446080/

    float3x3 w2oRotation;
    w2oRotation[0] = o2w[1].yzx * o2w[2].zxy - o2w[1].zxy * o2w[2].yzx;
    w2oRotation[1] = o2w[0].zxy * o2w[2].yzx - o2w[0].yzx * o2w[2].zxy;
    w2oRotation[2] = o2w[0].yzx * o2w[1].zxy - o2w[0].zxy * o2w[1].yzx;

    float det = dot(o2w[0].xyz, w2oRotation[0]);

    w2oRotation = transpose(w2oRotation);

    w2oRotation *= rcp(det);

    float3 w2oPosition = mul(w2oRotation, -o2w._14_24_34);

    float4x4 w2o;
    w2o._11_21_31_41 = float4(w2oRotation._11_21_31, 0.0f);
    w2o._12_22_32_42 = float4(w2oRotation._12_22_32, 0.0f);
    w2o._13_23_33_43 = float4(w2oRotation._13_23_33, 0.0f);
    w2o._14_24_34_44 = float4(w2oPosition, 1.0f);
    return w2o;
}

float LinearToGammaExact(float value)
{
    if (value <= 0.0F)
        return 0.0F;
    else if (value <= 0.0031308F)
        return 12.92F * value;
    else if (value < 1.0F)
        return 1.055F * pow(value, 0.4166667F) - 0.055F;
    else
        return pow(value, 0.45454545F);
}

// uses the exact (and more expansive) version of LinearToGammaSpace. Used only for baking.
float3 LinearToGamma(float3 linRGB)
{
    linRGB = max(linRGB, float3(0, 0, 0));
	
    // Exact version of the LineatToGammeSpace from UnityCG.cginc:
    return float3(LinearToGammaExact(linRGB.r), LinearToGammaExact(linRGB.g), LinearToGammaExact(linRGB.b));
}

#endif