from sudoku import *
import sys

def convert_cursor(out_row, out_col, in_row, in_col):
    x = (out_col * 181 + in_col * 60 + 11) + 30
    y = (out_row * 181 + in_row * 60 + 159) + 30

    return (x, y)


def callback(vmodel, frame, parameter):
    try:
        components = vmodel.Partition(frame)
        table = SUDOKU_TABLE(components)
        count = 0

        if not table.is_valid():
            vmodel.InitStopWatch = True
            return 'not loaded'

        while True:
            success, out_row, out_col, in_row, in_col, num = table.next()
            if not success:
                break

            count = count + 1
            coordination = convert_cursor(out_row, out_col, in_row, in_col)
            vmodel.App.Click('left', *coordination)

            x = int((num-1) * 516/9 + 23 + 55/2)
            y = 913
            vmodel.App.Click('left', x, y)

        if table.is_finish():
            return 'finish'

        if count == 0:
            vmodel.InitStopWatch = True
            return str(components)

        return 'Cannot find anymore'



    except Exception as e:
        return 'exception : ' + str(e)