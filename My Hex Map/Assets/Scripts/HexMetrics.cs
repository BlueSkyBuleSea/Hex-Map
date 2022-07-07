using UnityEngine;

public static class HexMetrics {

	public const float outerRadius = 10f;

	public const float innerRadius = outerRadius * 0.866025404f;

    //核心区域的大小占比
	public const float solidFactor = 0.8f;
    //边缘因子（用于混合）
    public const float blendFactor = 1f - solidFactor;

    //单位高度
	public const float elevationStep = 3f;

    //边缘矩形区域相关
    //平台数量
	public const int terracesPerSlope = 2;
    //绘制的步数
	public const int terraceSteps = terracesPerSlope * 2 + 1;
    //每步的水平分量
	public const float horizontalTerraceStepSize = 1f / terraceSteps;
    //每步的垂直分量
	public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);
    //扰动强度（纹理坐标范围0`1 -->-1~1  三个方向  最大位移 1*1*1 = 1.7  目前外径10，故需要将扰动放大）
	public const float cellPerturbStrength = 4f;
    //单元格整体的在Y方向的扰动强度
	public const float elevationPerturbStrength = 1.5f;
    //扰动的缩放因子 （使得一张扰动图 能覆盖更多的格子）
	public const float noiseScale = 0.003f;
	//六边形的顶点列表
	static Vector3[] corners = {
		new Vector3(0f, 0f, outerRadius),
		new Vector3(innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(0f, 0f, -outerRadius),
		new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
		new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
		new Vector3(0f, 0f, outerRadius)
	};
    //扰动图源
    public static Texture2D noiseSource;
    //扰动纹理采样
    public static Vector4 SampleNoise (Vector3 position) {
        //将世界坐标根当作UV坐标返回颜色
        return noiseSource.GetPixelBilinear(
			position.x * noiseScale,
			position.z * noiseScale
		);
	}

	public static Vector3 GetFirstCorner (HexDirection direction) {
		return corners[(int)direction];
	}

	public static Vector3 GetSecondCorner (HexDirection direction) {
		return corners[(int)direction + 1];
	}

	public static Vector3 GetFirstSolidCorner (HexDirection direction) {
		return corners[(int)direction] * solidFactor;
	}

	public static Vector3 GetSecondSolidCorner (HexDirection direction) {
		return corners[(int)direction + 1] * solidFactor;
	}

    //相邻三角面交接的矩形区域的高度
	public static Vector3 GetBridge (HexDirection direction) {
		return (corners[(int)direction] + corners[(int)direction + 1]) *
			blendFactor;
	}

	public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		a.x += (b.x - a.x) * h;
		a.z += (b.z - a.z) * h;
		float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
		a.y += (b.y - a.y) * v;
		return a;
	}

	public static Color TerraceLerp (Color a, Color b, int step) {
		float h = step * HexMetrics.horizontalTerraceStepSize;
		return Color.Lerp(a, b, h);
	}

    //获取边缘类型
	public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
		if (elevation1 == elevation2) {
			return HexEdgeType.Flat;
		}
		int delta = elevation2 - elevation1;
		if (delta == 1 || delta == -1) {
			return HexEdgeType.Slope;
		}
		return HexEdgeType.Cliff;
	}
}