using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour {

	public int width = 6; 
	public int height = 6;

	public Color defaultColor = Color.white;

	public HexCell cellPrefab;
	public Text cellLabelPrefab;

	HexCell[] cells;

	Canvas gridCanvas;
	HexMesh hexMesh;

	void Awake () {
		gridCanvas = GetComponentInChildren<Canvas>();
		hexMesh = GetComponentInChildren<HexMesh>();

		cells = new HexCell[height * width];

		for (int z = 0, i = 0; z < height; z++) {
			for (int x = 0; x < width; x++) {
				CreateCell(x, z, i++);
			}
		}
	}

	void Start () {
		hexMesh.Triangulate(cells);
	}

	public void ColorCell (Vector3 position, Color color) {
		position = transform.InverseTransformPoint(position);
		HexCoordinates coordinates = HexCoordinates.FromPosition(position);
		int index = coordinates.X + coordinates.Z * width + coordinates.Z / 2;
		HexCell cell = cells[index];
		cell.color = color;
		hexMesh.Triangulate(cells);
	}

    /// <summary>
    /// 创建单元格
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="i"></param>
	void CreateCell (int x, int z, int i) {
		Vector3 position;
		position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
		position.y = 0f;
		position.z = z * (HexMetrics.outerRadius * 1.5f);

		HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
		cell.transform.SetParent(transform, false);
        //世界坐标
		cell.transform.localPosition = position;
        //格子坐标
        cell.coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
		cell.color = defaultColor;

        //只用计算3个邻居  应为是镜像的
		if (x > 0) {
            //除每行首个外都有左邻居
			cell.SetNeighbor(HexDirection.W, cells[i - 1]);
		}
		if (z > 0) {
            //除每列首个外
            if ((z & 1) == 0) {
                //偶数列 都有右下邻居
                cell.SetNeighbor(HexDirection.SE, cells[i - width]);
                if (x > 0) {
                    //除开首个  都有坐下邻居
                    cell.SetNeighbor(HexDirection.SW, cells[i - width - 1]);
				}
			}
			else {
                //奇数列 都有坐下邻居
                cell.SetNeighbor(HexDirection.SW, cells[i - width]);
				if (x < width - 1) {
                    //最后一个除外 都有右下邻居
                    cell.SetNeighbor(HexDirection.SE, cells[i - width + 1]);
				}
			}
		}

        //显示格子信息的UI
		Text label = Instantiate<Text>(cellLabelPrefab);
		label.rectTransform.SetParent(gridCanvas.transform, false);
		label.rectTransform.anchoredPosition =
			new Vector2(position.x, position.z);
		label.text = cell.coordinates.ToStringOnSeparateLines();
	}
}