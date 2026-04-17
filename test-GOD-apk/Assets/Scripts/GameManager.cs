using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _depth = 4;
    [SerializeField] private Box _boxPrefab;

    private void Start()
    {
        //GenerateGrid();
    }

    private void GenerateGrid()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int z = 0; z < _depth; z++)
            {
                var box = Instantiate(_boxPrefab, new Vector3(x, 0, z), Quaternion.Euler(90, 0, 0));
            }
        }
    }
}
