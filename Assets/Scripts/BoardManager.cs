using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class BoardManager : MonoBehaviour
{
    public enum CubeFace
    {
        Front,
        Back,
        Right,
        Left,
        Top,
        Bottom
    }

    private enum TurnPhase
    {
        WaitingToStart,
        StartCountdown,
        PlayerMove,
        RotationChoice,
        Rotating,
        TurnDelay,
        GameOver
    }

    private enum RotationAction
    {
        None,
        RowLeft,
        RowRight,
        ColumnUp,
        ColumnDown
    }

    private class CubeletData
    {
        public Transform Transform;
        public Vector3Int Coordinate;
        public Vector3Int StartingCoordinate;
        public Quaternion StartingRotation;
    }

    private class CellData
    {
        public BoardUIButton Button;
        public Canvas Canvas;
        public CubeletData Cubelet;
        public TMP_Text TMPLabel;
        public Text LegacyLabel;
        public int Owner;
        public CubeFace Face;
        public int Row;
        public int Column;
    }

    [Header("Cube")]
    public Transform CubeRoot;
    public List<Transform> Cubelets = new List<Transform>();
    public float CubeletSpacing = 1f;
    public float RotationDuration = 0.5f;

    [Header("Match")]
    public float StartCountdownDuration = 5f;
    public float TurnDuration = 30f;
    public float TurnDelayDuration = 3f;
    public int BoardsToWin = 3;

    [Header("Player Colors")]
    public Color PlayerXColor = new Color(0.2f, 0.65f, 1f, 1f);
    public Color PlayerOColor = new Color(1f, 0.4f, 0.3f, 1f);

    [Header("Camera Background")]
    public Camera MainCamera;

    [Header("Rotation Choice")]
    public GameObject RotationChoicePanel;
    public UIButton RotationOptionOneButton;
    public UIButton RotationOptionTwoButton;
    public UIButton RotationRefocusButton;
    public Camera RotationChoiceCamera;
    public CameraController RotationChoiceOrbitController;
    public float RotationOptionWorldDistance = 0.85f;
    public float RotationOptionFaceOffset = 0.65f;

    [Header("Display")]
    public TMP_Text CurrentPlayerText;
    public Image CurrentPlayerPanel;
    [FormerlySerializedAs("TurnTimerText")]
    public TMP_Text TimerText;
    public TMP_Text StartTimerText;
    public TMP_Text PlayerXScoreText;
    public TMP_Text PlayerOScoreText;
    public TMP_Text MatchResultText;

    [Header("Events")]
    public UnityEvent StartGameEvent = new UnityEvent();
    public UnityEvent EndGameEvent = new UnityEvent();

    [Header("SFX")]
    public EventReference Player1Turn;
    public EventReference Player2Turn;
    public EventReference Rotate;
    public EventReference Player1BoardWin;
    public EventReference Player2BoardWin;
    public EventReference TimerTick;

    [Header("Board Win")]
    public GameObject BoardWinPrefab;
    public float BoardWinPrefabOffset = 0.05f;

    public int CurrentPlayer { get; private set; } = 1;
    public int PlayerXScore { get; private set; }
    public int PlayerOScore { get; private set; }
    public float TimeRemaining { get; private set; }
    public float ChoiceTimeRemaining { get; private set; }
    public float StartCountdownRemaining { get; private set; }

    private const float RotationChoiceDuration = 10f;
    private TurnPhase lastTickPhase = TurnPhase.WaitingToStart;
    private int lastTickSecond = -1;

    private readonly List<CubeletData> cubeletData = new List<CubeletData>();
    private readonly List<CellData> cells = new List<CellData>();
    private CellData[,,] faceCells = new CellData[6, 3, 3];

    private TurnPhase phase;
    private RotationAction choiceOne;
    private RotationAction choiceTwo;
    private CubeFace selectedFace;
    private int selectedRow;
    private int selectedColumn;
    private Image rotationChoicePanelImage;
    private RectTransform rotationChoicePanelRect;
    private Transform selectedCubelet;
    private Canvas rotationChoiceCanvas;
    private bool moveRotationPanelWithOptions;
    private Camera backgroundCamera;
    private CameraController rotationChoiceOrbitController;

    private void Awake()
    {
        if (CubeRoot == null || RotationChoicePanel == null ||
            RotationOptionOneButton == null || RotationOptionTwoButton == null)
        {
            Debug.LogError("BoardManager: Assign the cube root, rotation choice panel, and both rotation buttons.", this);
            enabled = false;
            return;
        }

        if (CubeletSpacing <= 0f)
        {
            Debug.LogError("BoardManager: Cubelet Spacing must be greater than zero.", this);
            enabled = false;
            return;
        }

        if (Cubelets.Count == 0)
        {
            foreach (Transform child in CubeRoot)
            {
                if (TryParseCubeletCoordinate(child.name, out _))
                    Cubelets.Add(child);
            }
        }

        if (Cubelets.Count != 26)
        {
            Debug.LogError("BoardManager: CubeRoot must have 26 cubelets, or you must assign exactly 26 Cubelets.", this);
            enabled = false;
            return;
        }

        HashSet<Vector3Int> usedCoordinates = new HashSet<Vector3Int>();

        for (int i = 0; i < Cubelets.Count; i++)
        {
            Transform cubelet = Cubelets[i];

            if (cubelet.parent != CubeRoot)
            {
                Debug.LogError("BoardManager: Every cubelet must be a direct child of CubeRoot.", this);
                enabled = false;
                return;
            }

            if (!TryParseCubeletCoordinate(cubelet.name, out Vector3Int coordinate))
            {
                Debug.LogError("BoardManager: Cubelet '" + cubelet.name + "' must use the XZY naming format (for example A1A or B3A). B2B is the missing center cubelet.", cubelet);
                enabled = false;
                return;
            }

            if (!usedCoordinates.Add(coordinate))
            {
                Debug.LogError("BoardManager: Cubelet '" + cubelet.name + "' duplicates an existing grid coordinate.", cubelet);
                enabled = false;
                return;
            }

            CubeletData data = new CubeletData
            {
                Transform = cubelet,
                Coordinate = coordinate,
                StartingCoordinate = coordinate,
                StartingRotation = cubelet.localRotation
            };

            cubeletData.Add(data);

            BoardUIButton[] buttons = cubelet.GetComponentsInChildren<BoardUIButton>(true);

            for (int j = 0; j < buttons.Length; j++)
            {
                BoardUIButton button = buttons[j];
                Canvas canvas = button.GetComponentInParent<Canvas>();

                if (canvas == null)
                {
                    Debug.LogError("BoardManager: Each playable button needs a Canvas on its cubelet.", button);
                    enabled = false;
                    return;
                }

                CellData cell = new CellData
                {
                    Button = button,
                    Canvas = canvas,
                    Cubelet = data,
                    TMPLabel = button.GetComponentInChildren<TMP_Text>(true),
                    LegacyLabel = button.GetComponentInChildren<Text>(true)
                };

                cells.Add(cell);
                button.onClick.AddListener(() => PlaceMark(cell));
            }
        }

        if (cells.Count != 54)
        {
            Debug.LogError("BoardManager: The 26 cubelets must contain exactly 54 playable buttons.", this);
            enabled = false;
            return;
        }

        RotationOptionOneButton.onClick.AddListener(ChooseRotationOne);
        RotationOptionTwoButton.onClick.AddListener(ChooseRotationTwo);
        if (RotationRefocusButton != null)
            RotationRefocusButton.onClick.AddListener(RefocusRotationCamera);
        rotationChoicePanelImage = RotationChoicePanel.GetComponent<Image>();
        rotationChoicePanelRect = RotationChoicePanel.GetComponent<RectTransform>();
        rotationChoiceCanvas = RotationOptionOneButton.GetComponentInParent<Canvas>(true);

        if (rotationChoiceCanvas == null || rotationChoiceCanvas.renderMode != RenderMode.WorldSpace ||
            RotationOptionTwoButton.GetComponentInParent<Canvas>(true) != rotationChoiceCanvas)
        {
            Debug.LogError("BoardManager: Both rotation-option buttons must share the same World Space Canvas.", RotationOptionOneButton);
            enabled = false;
            return;
        }

        moveRotationPanelWithOptions = RotationChoicePanel.transform == rotationChoiceCanvas.transform ||
            RotationChoicePanel.transform.IsChildOf(rotationChoiceCanvas.transform);

        Camera sceneCamera = RotationChoiceCamera != null ? RotationChoiceCamera : Camera.main;
        if (rotationChoiceCanvas.worldCamera == null && sceneCamera != null)
            rotationChoiceCanvas.worldCamera = sceneCamera;

        rotationChoiceOrbitController = RotationChoiceOrbitController;
        if (rotationChoiceOrbitController == null)
        {
            Camera orbitCamera = RotationChoiceCamera != null ? RotationChoiceCamera :
                MainCamera != null ? MainCamera : Camera.main;

            if (orbitCamera != null)
                rotationChoiceOrbitController = orbitCamera.GetComponent<CameraController>();
        }

        backgroundCamera = MainCamera != null ? MainCamera : Camera.main;
        if (backgroundCamera != null)
            backgroundCamera.clearFlags = CameraClearFlags.SolidColor;

        RestartGame();
    }

    private static bool TryParseCubeletCoordinate(string cubeletName, out Vector3Int coordinate)
    {
        coordinate = Vector3Int.zero;

        if (cubeletName.Length != 3)
            return false;

        char x = char.ToUpperInvariant(cubeletName[0]);
        char z = cubeletName[1];
        char y = char.ToUpperInvariant(cubeletName[2]);

        if (x < 'A' || x > 'C' || z < '1' || z > '3' || y < 'A' || y > 'C')
            return false;

        coordinate = new Vector3Int(x - 'B', 'B' - y, '2' - z);
        return coordinate != Vector3Int.zero;
    }

    private void Update()
    {
        if (phase == TurnPhase.PlayerMove)
        {
            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                BeginTurnDelay();
            }
        }
        else if (phase == TurnPhase.RotationChoice)
        {
            ChoiceTimeRemaining -= Time.deltaTime;

            if (ChoiceTimeRemaining <= 0f)
            {
                ChoiceTimeRemaining = 0f;
                ChooseRotation(Random.Range(0, 2) == 0 ? choiceOne : choiceTwo);
            }
        }

        UpdateCountdownTick();
        UpdateDisplay();
    }

    private void LateUpdate()
    {
        if (phase == TurnPhase.RotationChoice)
            PositionRotationOptions();
    }

    public void RestartGame()
    {
        StopAllCoroutines();

        if (rotationChoiceOrbitController != null)
            rotationChoiceOrbitController.ReleaseFaceFocus();

        for (int i = 0; i < cubeletData.Count; i++)
        {
            CubeletData cubelet = cubeletData[i];
            cubelet.Coordinate = cubelet.StartingCoordinate;
            cubelet.Transform.localPosition = (Vector3)cubelet.Coordinate * CubeletSpacing;
            cubelet.Transform.localRotation = cubelet.StartingRotation;
        }

        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].Owner = 0;
            UpdateCellLabel(cells[i]);
        }

        PlayerXScore = 0;
        PlayerOScore = 0;
        CurrentPlayer = 1;
        selectedCubelet = null;
        ChoiceTimeRemaining = 0f;
        TimeRemaining = Mathf.Max(0.1f, TurnDuration);
        StartCountdownRemaining = 0f;
        StartTimerText.text = "";
        phase = TurnPhase.WaitingToStart;

        SetRotationChoiceVisible(false);
        if (MatchResultText != null)
            MatchResultText.text = "";

        RefreshFaceCells();

        if (!ValidateFaceCells())
        {
            phase = TurnPhase.GameOver;
            enabled = false;
            return;
        }

        UpdateButtonStates();
        UpdateRefocusButtonColor();
        UpdateDisplay();
    }

    public void StartGame()
    {
        RestartGame();
        if (!enabled)
            return;

        StartCountdownRemaining = Mathf.Max(0f, StartCountdownDuration);
        phase = TurnPhase.StartCountdown;
        UpdateButtonStates();
        UpdateDisplay();
        StartCoroutine(StartGameCountdown());
    }

    private IEnumerator StartGameCountdown()
    {
        int lastStartSecond = -1;

        while (StartCountdownRemaining > 0f)
        {
            int second = Mathf.CeilToInt(StartCountdownRemaining);
            StartTimerText.text = second.ToString();

            if (second != lastStartSecond)
            {
                PlayTimerTick();
                lastStartSecond = second;
            }
            yield return null;
            StartCountdownRemaining = Mathf.Max(0f, StartCountdownRemaining - Time.deltaTime);
        }

        StartTimerText.text = "GO";
        yield return new WaitForSeconds(1f);
        StartTimerText.text = "";

        TimeRemaining = Mathf.Max(0.1f, TurnDuration);
        phase = TurnPhase.PlayerMove;
        UpdateButtonStates();
        UpdateDisplay();
        StartGameEvent.Invoke();
        PlayTurnSFX();
    }

    private void PlaceMark(CellData cell)
    {
        if (phase != TurnPhase.PlayerMove || cell.Owner != 0)
            return;

        cell.Owner = CurrentPlayer;
        UpdateCellLabel(cell);
        UpdateButtonStates();

        selectedFace = cell.Face;
        selectedCubelet = cell.Cubelet.Transform;
        selectedRow = cell.Row;
        selectedColumn = cell.Column;

        GetRotationOptions(selectedRow, selectedColumn, out choiceOne, out choiceTwo);

        if (choiceOne == RotationAction.None)
        {
            ResolveMove();
        }
        else if (choiceTwo == RotationAction.None)
        {
            StartCoroutine(RotateLayer(choiceOne));
        }
        else
        {
            phase = TurnPhase.RotationChoice;
            RefocusRotationCamera();
            ChoiceTimeRemaining = RotationChoiceDuration;
            SetButtonLabel(RotationOptionOneButton, GetRotationLabel(choiceOne));
            SetButtonLabel(RotationOptionTwoButton, GetRotationLabel(choiceTwo));
            if (RotationRefocusButton != null)
                SetButtonLabel(RotationRefocusButton, "Reorient Camera");
            SetRotationChoiceVisible(true);
            UpdateRefocusButtonColor();
            PositionRotationOptions();
            UpdateButtonStates();
            UpdateDisplay();
        }
    }

    public void RefocusRotationCamera()
    {
        if (phase != TurnPhase.RotationChoice || rotationChoiceOrbitController == null)
            return;

        GetFaceAxes(selectedFace, out _, out Vector3Int faceUp);
        Vector3 focusUp = CubeRoot.TransformDirection((Vector3)faceUp);

        if (selectedFace == CubeFace.Bottom)
            focusUp = -focusUp;

        rotationChoiceOrbitController.FocusOnFace(
            CubeRoot.TransformDirection((Vector3)GetFaceNormal(selectedFace)), focusUp);
    }

    public void ChooseRotationOne()
    {
        if (phase == TurnPhase.RotationChoice)
            ChooseRotation(choiceOne);
    }

    public void ChooseRotationTwo()
    {
        if (phase == TurnPhase.RotationChoice)
            ChooseRotation(choiceTwo);
    }

    private void ChooseRotation(RotationAction action)
    {
        SetRotationChoiceVisible(false);
        ChoiceTimeRemaining = 0f;
        StartCoroutine(RotateLayer(action));
    }

    private IEnumerator RotateLayer(RotationAction action)
    {
        phase = TurnPhase.Rotating;
        UpdateButtonStates();

        if (!Rotate.IsNull)
            RuntimeManager.PlayOneShot(Rotate);

        GetFaceAxes(selectedFace, out Vector3Int right, out Vector3Int up);

        bool isRow = action == RotationAction.RowLeft || action == RotationAction.RowRight;
        Vector3Int axis = isRow ? up : right;
        int layer = isRow ? 1 - selectedRow : selectedColumn - 1;
        float degrees = action == RotationAction.RowLeft || action == RotationAction.ColumnUp ? -90f : 90f;

        List<CubeletData> moving = new List<CubeletData>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Quaternion> startRotations = new List<Quaternion>();

        for (int i = 0; i < cubeletData.Count; i++)
        {
            CubeletData cubelet = cubeletData[i];

            if (Mathf.RoundToInt(Vector3.Dot(cubelet.Coordinate, axis)) != layer)
                continue;

            moving.Add(cubelet);
            startPositions.Add(cubelet.Transform.localPosition);
            startRotations.Add(cubelet.Transform.localRotation);
        }

        float duration = Mathf.Max(0f, RotationDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            Quaternion step = Quaternion.AngleAxis(degrees * eased, (Vector3)axis);

            for (int i = 0; i < moving.Count; i++)
            {
                moving[i].Transform.localPosition = step * startPositions[i];
                moving[i].Transform.localRotation = step * startRotations[i];
            }

            yield return null;
        }

        Quaternion finalTurn = Quaternion.AngleAxis(degrees, (Vector3)axis);

        for (int i = 0; i < moving.Count; i++)
        {
            CubeletData cubelet = moving[i];
            Vector3 turned = finalTurn * (Vector3)cubelet.Coordinate;

            cubelet.Coordinate = new Vector3Int(
                Mathf.RoundToInt(turned.x),
                Mathf.RoundToInt(turned.y),
                Mathf.RoundToInt(turned.z));

            cubelet.Transform.localPosition = (Vector3)cubelet.Coordinate * CubeletSpacing;
            cubelet.Transform.localRotation = finalTurn * startRotations[i];
        }

        RefreshFaceCells();

        if (!ValidateFaceCells())
        {
            phase = TurnPhase.GameOver;
            enabled = false;
            yield break;
        }

        ResolveMove();
    }

    private void ResolveMove()
    {
        if (rotationChoiceOrbitController != null)
            rotationChoiceOrbitController.ReleaseFaceFocus();

        CountWinsAndClearBoards();

        bool xWonMatch = PlayerXScore >= Mathf.Max(1, BoardsToWin);
        bool oWonMatch = PlayerOScore >= Mathf.Max(1, BoardsToWin);

        if (xWonMatch || oWonMatch)
        {
            phase = TurnPhase.GameOver;

            if (MatchResultText != null)
            {
                MatchResultText.text = xWonMatch && oWonMatch
                    ? "Match tied!"
                    : xWonMatch ? "Player X wins!" : "Player O wins!";
            }

            SetRotationChoiceVisible(false);
            UpdateButtonStates();
            UpdateDisplay();
            StartCoroutine(EndGameDelay());
            return;
        }

        BeginTurnDelay();
    }

    private IEnumerator EndGameDelay()
    {
        yield return new WaitForSeconds(10f);
        EndGameEvent.Invoke();
    }

    private void BeginTurnDelay()
    {
        phase = TurnPhase.TurnDelay;
        TimeRemaining = 0f;
        ChoiceTimeRemaining = 0f;
        SetRotationChoiceVisible(false);
        UpdateButtonStates();
        UpdateDisplay();
        StartCoroutine(WaitBetweenTurns());
    }

    private IEnumerator WaitBetweenTurns()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, TurnDelayDuration));
        BeginNextTurn();
    }

    private void BeginNextTurn()
    {
        CurrentPlayer = CurrentPlayer == 1 ? 2 : 1;
        TimeRemaining = Mathf.Max(0.1f, TurnDuration);
        ChoiceTimeRemaining = 0f;
        phase = TurnPhase.PlayerMove;
        SetRotationChoiceVisible(false);
        UpdateButtonStates();
        UpdateRefocusButtonColor();
        UpdateDisplay();
        PlayTurnSFX();
    }

    private void UpdateCountdownTick()
    {
        if (phase != TurnPhase.PlayerMove && phase != TurnPhase.RotationChoice)
        {
            lastTickPhase = phase;
            lastTickSecond = -1;
            return;
        }

        int second = Mathf.CeilToInt(phase == TurnPhase.PlayerMove ? TimeRemaining : ChoiceTimeRemaining);

        if (lastTickPhase != phase)
        {
            lastTickPhase = phase;
            lastTickSecond = -1;
        }

        if (second != lastTickSecond)
        {
            if (second >= 1 && second < 5)
                PlayTimerTick();

            lastTickSecond = second;
        }
    }

    private void PlayTimerTick()
    {
        if (!TimerTick.IsNull)
            RuntimeManager.PlayOneShot(TimerTick);
    }

    private void PlayTurnSFX()
    {
        EventReference turnEvent = CurrentPlayer == 1 ? Player1Turn : Player2Turn;

        if (!turnEvent.IsNull)
            RuntimeManager.PlayOneShot(turnEvent);
    }

    private bool CountWinsAndClearBoards()
    {
        bool boardWasWon = false;

        for (int face = 0; face < 6; face++)
        {
            bool xWins = HasLine(face, 1);
            bool oWins = HasLine(face, 2);

            if (!xWins && !oWins)
                continue;

            boardWasWon = true;

            if (xWins)
            {
                PlayerXScore++;

                if (!Player1BoardWin.IsNull)
                    RuntimeManager.PlayOneShot(Player1BoardWin);
            }

            if (oWins)
            {
                PlayerOScore++;

                if (!Player2BoardWin.IsNull)
                    RuntimeManager.PlayOneShot(Player2BoardWin);
            }

            if (BoardWinPrefab != null)
            {
                CubeFace winningFace = (CubeFace)face;
                GetFaceAxes(winningFace, out _, out Vector3Int faceUp);
                Vector3 normal = CubeRoot.TransformDirection((Vector3)GetFaceNormal(winningFace));
                Quaternion rotation = Quaternion.LookRotation(normal, CubeRoot.TransformDirection((Vector3)faceUp));
                Vector3 position = faceCells[face, 1, 1].Canvas.transform.position + normal * BoardWinPrefabOffset;
                Instantiate(BoardWinPrefab, position, rotation);
            }

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    CellData cell = faceCells[face, row, column];
                    cell.Owner = 0;
                    UpdateCellLabel(cell);
                }
            }
        }

        return boardWasWon;
    }

    private bool HasLine(int face, int player)
    {
        for (int i = 0; i < 3; i++)
        {
            if (OwnedBy(face, i, 0, player) && OwnedBy(face, i, 1, player) && OwnedBy(face, i, 2, player))
                return true;

            if (OwnedBy(face, 0, i, player) && OwnedBy(face, 1, i, player) && OwnedBy(face, 2, i, player))
                return true;
        }

        return (OwnedBy(face, 0, 0, player) && OwnedBy(face, 1, 1, player) && OwnedBy(face, 2, 2, player)) ||
               (OwnedBy(face, 0, 2, player) && OwnedBy(face, 1, 1, player) && OwnedBy(face, 2, 0, player));
    }

    private bool OwnedBy(int face, int row, int column, int player)
    {
        CellData cell = faceCells[face, row, column];
        return cell != null && cell.Owner == player;
    }

    private void RefreshFaceCells()
    {
        faceCells = new CellData[6, 3, 3];

        for (int i = 0; i < cells.Count; i++)
        {
            CellData cell = cells[i];
            Vector3 normal = CubeRoot.InverseTransformDirection(cell.Canvas.transform.forward);
            cell.Face = GetFaceFromNormal(normal);

            GetFaceAxes(cell.Face, out Vector3Int right, out Vector3Int up);

            cell.Row = 1 - Mathf.RoundToInt(Vector3.Dot(cell.Cubelet.Coordinate, up));
            cell.Column = 1 + Mathf.RoundToInt(Vector3.Dot(cell.Cubelet.Coordinate, right));

            if (cell.Row < 0 || cell.Row > 2 || cell.Column < 0 || cell.Column > 2)
            {
                Debug.LogError("BoardManager: A button does not align with its cube face. Check Canvas forward directions.", cell.Button);
                continue;
            }

            faceCells[(int)cell.Face, cell.Row, cell.Column] = cell;
        }
    }

    private bool ValidateFaceCells()
    {
        for (int face = 0; face < 6; face++)
        {
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    if (faceCells[face, row, column] != null)
                        continue;

                    Debug.LogError("BoardManager: A face position is missing. Check cubelet spacing, cubelet positions, and Canvas orientations.", this);
                    return false;
                }
            }
        }

        return true;
    }

    private static CubeFace GetFaceFromNormal(Vector3 normal)
    {
        Vector3 absolute = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));

        if (absolute.x >= absolute.y && absolute.x >= absolute.z)
            return normal.x > 0f ? CubeFace.Right : CubeFace.Left;

        if (absolute.y >= absolute.z)
            return normal.y > 0f ? CubeFace.Top : CubeFace.Bottom;

        return normal.z > 0f ? CubeFace.Front : CubeFace.Back;
    }

    private static void GetFaceAxes(CubeFace face, out Vector3Int right, out Vector3Int up)
    {
        switch (face)
        {
            case CubeFace.Front:
                right = Vector3Int.right;
                up = Vector3Int.up;
                break;
            case CubeFace.Back:
                right = Vector3Int.left;
                up = Vector3Int.up;
                break;
            case CubeFace.Right:
                right = new Vector3Int(0, 0, -1);
                up = Vector3Int.up;
                break;
            case CubeFace.Left:
                right = Vector3Int.forward;
                up = Vector3Int.up;
                break;
            case CubeFace.Top:
                right = Vector3Int.right;
                up = new Vector3Int(0, 0, -1);
                break;
            default:
                right = Vector3Int.right;
                up = Vector3Int.forward;
                break;
        }
    }

    private static void GetRotationOptions(int row, int column, out RotationAction first, out RotationAction second)
    {
        first = RotationAction.None;
        second = RotationAction.None;

        if (row == 0)
        {
            if (column == 0)
            {
                first = RotationAction.RowLeft;
                second = RotationAction.ColumnUp;
            }
            else if (column == 1)
            {
                first = RotationAction.ColumnUp;
            }
            else
            {
                first = RotationAction.RowRight;
                second = RotationAction.ColumnUp;
            }
        }
        else if (row == 1)
        {
            if (column == 0)
                first = RotationAction.RowLeft;
            else if (column == 2)
                first = RotationAction.RowRight;
        }
        else
        {
            if (column == 0)
            {
                first = RotationAction.RowLeft;
                second = RotationAction.ColumnDown;
            }
            else if (column == 1)
            {
                first = RotationAction.ColumnDown;
            }
            else
            {
                first = RotationAction.RowRight;
                second = RotationAction.ColumnDown;
            }
        }
    }

    private void PositionRotationOptions()
    {
        Camera sceneCamera = RotationChoiceCamera != null ? RotationChoiceCamera : Camera.main;
        if (sceneCamera == null)
            return;

        GetFaceAxes(selectedFace, out Vector3Int right, out Vector3Int up);

        Vector3 faceRight = CubeRoot.TransformDirection((Vector3)right);
        Vector3 faceUp = CubeRoot.TransformDirection((Vector3)up);
        Vector3 faceNormal = CubeRoot.TransformDirection((Vector3)GetFaceNormal(selectedFace));
        Vector3 choiceCenter = selectedCubelet.position + faceNormal * Mathf.Max(0f, RotationOptionFaceOffset);
        Quaternion billboardRotation = sceneCamera.transform.rotation;

        if (moveRotationPanelWithOptions)
        {
            rotationChoicePanelRect.position = choiceCenter;
            rotationChoicePanelRect.rotation = billboardRotation;
        }

        PlaceRotationOption(RotationOptionOneButton, choiceCenter,
            GetOptionDirection(choiceOne, faceRight, faceUp), choiceOne, billboardRotation, sceneCamera.transform);
        PlaceRotationOption(RotationOptionTwoButton, choiceCenter,
            GetOptionDirection(choiceTwo, faceRight, faceUp), choiceTwo, billboardRotation, sceneCamera.transform);

        if (RotationRefocusButton != null &&
            RotationRefocusButton.GetComponentInParent<Canvas>(true) == rotationChoiceCanvas)
        {
            RectTransform refocusRect = RotationRefocusButton.GetComponent<RectTransform>();
            refocusRect.anchorMin = new Vector2(0.5f, 0.5f);
            refocusRect.anchorMax = new Vector2(0.5f, 0.5f);
            refocusRect.pivot = new Vector2(0.5f, 0.5f);
            refocusRect.position = choiceCenter;
            refocusRect.rotation = billboardRotation;
        }
    }

    private void SetRotationChoiceVisible(bool visible)
    {
        RotationChoicePanel.SetActive(visible);

        if (!RotationOptionOneButton.transform.IsChildOf(RotationChoicePanel.transform))
            RotationOptionOneButton.gameObject.SetActive(visible);

        if (!RotationOptionTwoButton.transform.IsChildOf(RotationChoicePanel.transform))
            RotationOptionTwoButton.gameObject.SetActive(visible);

        if (RotationRefocusButton != null)
            RotationRefocusButton.gameObject.SetActive(visible);
    }

    private static Vector3Int GetFaceNormal(CubeFace face)
    {
        switch (face)
        {
            case CubeFace.Front: return Vector3Int.forward;
            case CubeFace.Back: return Vector3Int.back;
            case CubeFace.Right: return Vector3Int.right;
            case CubeFace.Left: return Vector3Int.left;
            case CubeFace.Top: return Vector3Int.up;
            default: return Vector3Int.down;
        }
    }

    private static Vector3 GetOptionDirection(RotationAction action, Vector3 faceRight, Vector3 faceUp)
    {
        switch (action)
        {
            case RotationAction.RowLeft: return -faceRight;
            case RotationAction.RowRight: return faceRight;
            case RotationAction.ColumnUp: return faceUp;
            case RotationAction.ColumnDown: return -faceUp;
            default: return Vector3.zero;
        }
    }

    private void PlaceRotationOption(UIButton button, Vector3 center, Vector3 direction, RotationAction action, Quaternion billboardRotation, Transform cameraTransform)
    {
        Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, cameraTransform.forward);

        if (projectedDirection.sqrMagnitude < 0.0001f)
        {
            projectedDirection = action == RotationAction.RowLeft ? -cameraTransform.right :
                action == RotationAction.RowRight ? cameraTransform.right :
                action == RotationAction.ColumnUp ? cameraTransform.up : -cameraTransform.up;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.position = center + projectedDirection.normalized * Mathf.Max(0f, RotationOptionWorldDistance);
        rect.rotation = billboardRotation;
    }

    private string GetRotationLabel(RotationAction action)
    {
        bool invertArrows = selectedFace == CubeFace.Bottom;

        switch (action)
        {
            case RotationAction.RowLeft:
                return invertArrows ? "Rotate ←" : "Rotate →";
            case RotationAction.RowRight:
                return invertArrows ? "Rotate →" : "Rotate ←";
            case RotationAction.ColumnUp:
                return invertArrows ? "Rotate ↓" : "Rotate ↑";
            case RotationAction.ColumnDown:
                return invertArrows ? "Rotate ↑" : "Rotate ↓";
            default:
                return "No Rotation";
        }
    }

    private void UpdateButtonStates()
    {
        bool canPlay = phase == TurnPhase.PlayerMove;

        for (int i = 0; i < cells.Count; i++)
            cells[i].Button.SetBoardState(CurrentPlayer, cells[i].Owner, PlayerXColor, PlayerOColor, canPlay && cells[i].Owner == 0);
    }

    private static void UpdateCellLabel(CellData cell)
    {
        string value = cell.Owner == 1 ? "X" : cell.Owner == 2 ? "O" : "";

        if (cell.TMPLabel != null)
            cell.TMPLabel.text = value;
        else if (cell.LegacyLabel != null)
            cell.LegacyLabel.text = value;
    }

    private void UpdateRefocusButtonColor()
    {
        if (RotationRefocusButton != null)
        {
            Color playerColor = CurrentPlayer == 1 ? PlayerXColor : PlayerOColor;
            SetRefocusButtonColor(RotationRefocusButton, playerColor);
        }
    }

    private static void SetRefocusButtonColor(UIButton button, Color color)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
            image = button.GetComponentInChildren<Image>(true);

        if (image != null)
        {
            button.targetGraphic = image;
            image.color = Color.white;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.2f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = color;
        colors.colorMultiplier = 1f;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;
    }

    private static void SetButtonLabel(UIButton button, string value)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);

        if (tmp != null)
        {
            tmp.text = value;
            return;
        }

        Text legacy = button.GetComponentInChildren<Text>(true);
        if (legacy != null)
            legacy.text = value;
    }

    private void UpdateDisplay()
    {
        if (CurrentPlayerText != null)
            CurrentPlayerText.text = CurrentPlayer == 1 ? "Player 1" : "Player 2";

        Color currentPlayerColor = CurrentPlayer == 1 ? PlayerXColor : PlayerOColor;

        if (backgroundCamera != null)
            backgroundCamera.backgroundColor = currentPlayerColor;

        if (CurrentPlayerPanel != null)
            CurrentPlayerPanel.color = currentPlayerColor;

        if (rotationChoicePanelImage != null)
            rotationChoicePanelImage.color = currentPlayerColor;

        if (TimerText != null)
            TimerText.text = phase == TurnPhase.PlayerMove ? Mathf.CeilToInt(TimeRemaining).ToString() :
                phase == TurnPhase.RotationChoice ? Mathf.CeilToInt(ChoiceTimeRemaining).ToString() : "--";

        if (PlayerXScoreText != null)
            PlayerXScoreText.text = PlayerXScore.ToString();

        if (PlayerOScoreText != null)
            PlayerOScoreText.text = PlayerOScore.ToString();
    }
}
