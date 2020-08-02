namespace zLib {
	interface ISelectable {
		bool IsSelected { get; set; }
		bool IsExpanded { get; set; }
        int Depth { get; set; }
	}
}
