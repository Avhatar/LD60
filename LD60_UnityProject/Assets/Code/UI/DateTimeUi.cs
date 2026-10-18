using System;
using LD60;
using LD60.Settings;
using TMPro;
using UnityEngine;

public class DateTimeUi : MonoBehaviour
{
    [SerializeField]
    private TMP_Text dateTimeText;

    [SerializeField]
    private string _separator = "  ->  ";

    [SerializeField]
    private Color _endDateColor = new Color(1f, 0.37f, 0.37f, 1f);

    private int _shownYear = -1;
    private int _shownMonth = -1;
    private int _shownDay = -1;
    private int _shownEndYear = -1;
    private int _shownEndMonth = -1;
    private int _shownEndDay = -1;
    private string _endDateColorHex = "FF5E5EFF";

    private void Start()
    {
        if (dateTimeText == null)
        {
            Debug.LogError($"{nameof(DateTimeUi)}: {nameof(dateTimeText)} is null in {nameof(Start)} on '{name}'. " +
                           "Required to draw the date the run has reached. " +
                           "Fix: assign a TextMeshPro text object to the Date Time Text slot on this object.", this);
            enabled = false;
            return;
        }

        _endDateColorHex = ColorUtility.ToHtmlStringRGBA(_endDateColor);
        Refresh();
    }

    private void OnValidate()
    {
        _endDateColorHex = ColorUtility.ToHtmlStringRGBA(_endDateColor);
        _shownDay = -1;
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        DateTime date = Core.CurrentDate;
        ActPeriod act = Core.CurrentAct;
        GameDate end = act == null ? null : act.End;

        int endYear = end == null ? -1 : end.Year;
        int endMonth = end == null ? -1 : end.Month;
        int endDay = end == null ? -1 : end.Day;

        if (date.Year == _shownYear && date.Month == _shownMonth && date.Day == _shownDay &&
            endYear == _shownEndYear && endMonth == _shownEndMonth && endDay == _shownEndDay)
            return;

        _shownYear = date.Year;
        _shownMonth = date.Month;
        _shownDay = date.Day;
        _shownEndYear = endYear;
        _shownEndMonth = endMonth;
        _shownEndDay = endDay;

        if (end == null)
        {
            dateTimeText.text = string.Format("{0:00}.{1:00}.{2:0000}", _shownDay, _shownMonth, _shownYear);
            return;
        }

        dateTimeText.text = string.Format("{0:00}.{1:00}.{2:0000}{3}<color=#{4}>{5:00}.{6:00}.{7:0000}</color>",
                                          _shownDay, _shownMonth, _shownYear, _separator, _endDateColorHex,
                                          _shownEndDay, _shownEndMonth, _shownEndYear);
    }
}
