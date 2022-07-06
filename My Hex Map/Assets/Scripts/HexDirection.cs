public enum HexDirection {
    //右上、右、右下、坐下、左、左上
    NE, E, SE, SW, W, NW
}

public static class HexDirectionExtensions {

    //镜像
    public static HexDirection Opposite (this HexDirection direction) {
		return (int)direction < 3 ? (direction + 3) : (direction - 3);
	}
    //前向
    public static HexDirection Previous (this HexDirection direction) {
		return direction == HexDirection.NE ? HexDirection.NW : (direction - 1);
	}
    //后向
    public static HexDirection Next (this HexDirection direction) {
		return direction == HexDirection.NW ? HexDirection.NE : (direction + 1);
	}
}