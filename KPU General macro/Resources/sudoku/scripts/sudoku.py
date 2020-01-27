def combinations(iterable, r):
    pool = tuple(iterable)
    n = len(pool)
    if r > n:
        return
    indices = list(range(r))
    yield tuple(pool[i] for i in indices)
    while True:
        for i1 in reversed(range(r)):
            if indices[i1] != i1 + n - r:
                break
        else:
            return
        indices[i1] += 1
        for i2 in range(i1+1, r):
            indices[i2] = indices[i2-1] + 1
            yield tuple(pool[i] for i in indices)


class cell:
    def __init__(self, num=None, candidates=[]):
        self._num = num
        self._candidates = candidates

    def clone(self):
        cell = cell(self._num, list(self._candidates))
        return cell

    def erase_candidate(self, value):
        if type(value) is list or type(value) is set:
            for v in value:
                self.erase_candidate(v)

        elif type(value) is int:
            if value in self._candidates:
                self._candidates.remove(value)

        if not self.is_valid():
            raise Exception('invalid cell')

    def remain_candidates(self, value):
        for candidate in self._candidates:
            if candidate in value:
                continue

            self._candidates.remove(candidate)

        if not self.is_valid():
            raise Exception('invalid cell')

    def is_valid(self):
        if self._num is None and len(self._candidates) == 0:
            return False

        return True

    def has_candidates(self):
        return self._num is None

    def has_one_candidate(self):
        return len(self._candidates) == 1

    def get_candidate(self):
        if not self.has_one_candidate():
            return None

        return self._candidates[0]

    def contains_candidate(self, value):
        if type(value) is int:
            return value in self._candidates
        elif type(value) is list or type(value) is set:
            for v in value:
                if not self.contains_candidate(v):
                    return False
            return True

    def get_value(self):
        return self._num

class SUDOKU_TABLE:
    def __init__(self, components, root=None):
        self._cells = [[[[None, None, None], [None, None, None], [None, None, None]], [[None, None, None], [None, None, None], [None, None, None]], [[None, None, None], [None, None, None], [None, None, None]]], [[[None, None, None], [None, None, None], [None, None, None]], [[None, None, None], [None, None, None], [None, None, None]], [[None, None, None], [None, None, None], [None, None, None]]], [[[None, None, None], [None, None, None], [None, None, None]], [[None, None, None], [None, None, None], [None, None, None]], [[None, None, None], [None, None, None], [None, None, None]]]]
        self._root = root if root is not None else self
        self._current = None
        
        if components is not None:
            self.foreach_all(self.on_init, components)

    def on_init(self, out_row, out_col, in_row, in_col, components):
        if type(components[out_row][out_col][in_row][in_col]) is int:
                self._cells[out_row][out_col][in_row][in_col] = cell(components[out_row][out_col][in_row][in_col])
        else:
            self._cells[out_row][out_col][in_row][in_col] = cell(None, components[out_row][out_col][in_row][in_col])
                        
    def __getitem__(self, position):
        out_row, out_col, in_row, in_col = position
        return self._cells[out_row][out_col][in_row][in_col]

    def clone(self, root):
        table = SUDOKU_TABLE(None, root)
        for i in range(3**4):
            out_row = (i // 27)
            out_col = (i // 9) % 3
            in_row  = (i // 3) % 3
            in_col  = (i % 3)

            table._cells[out_row][out_col][in_row][in_col] = self[out_row, out_col, in_row, in_col].clone()
        return table

    def erase_in_row(self, out_row, in_row, num, f=None):
        for col in range(3**2):
            out_col = col // 3
            in_col  = col %  3
            if f is not None and not f(self, out_row, out_col, in_row, in_col):
                return
            
            self[out_row, out_col, in_row, in_col].erase_candidate(num)

    def erase_in_column(self, out_col, in_col, num, f=None):
        for row in range(3**2):
            out_row = row // 3
            in_row  = row %  3
            
            if f is not None and not f(self, out_row, out_col, in_row, in_col):
                return

            self[out_row, out_col, in_row, in_col].erase_candidate(num)

    def erase_in_box(self, out_row, out_col, num, f=None):
        for i in range(3**2):
            in_row = i // 3
            in_col = i %  3
            
            if f is not None and not f(self, out_row, out_col, in_row, in_col):
                return

            self[out_row, out_col, in_row, in_col].erase_candidate(num)

    def contains_inner_box(self, row, col, num):
        for in_row in range(3):
            for in_col in range(3):
                value = self.__getitem__((row, col, in_row, in_col)).get_value()
                if value == num:
                    return True

        return False

    def contains_row(self, out_row, in_row, num):
        for out_col in range(3):
            for in_col in range(3):
                value = self[out_row, out_col, in_row, in_col].get_value()
                if value == num:
                    return True

        return False

    def contains_column(self, out_col, in_col, num):
        for out_row in range(3):
            for in_row in range(3):
                value = self[out_row, out_col, in_row, in_col].get_value()
                if value == num:
                    return True

        return False

    def foreach_all(self, f, param=None):
        for i in range(3**4):
            out_row = (i // 27)
            out_col = (i // 9) % 3
            in_row  = (i // 3) % 3
            in_col  = (i % 3)

            ret = f(out_row, out_col, in_row, in_col, param)
            if ret is not None:
                return ret


    def foreach_out(self, f, param=None):
        for row in range(3):
            for col in range(3):
                ret = f(row, col, param)
                if ret is not None:
                    return ret

        return None

    def foreach_inbox(self, f, out_row, out_col, param=None):
        for row in range(3):
            for col in range(3):
                ret = f(out_row, out_col, row, col, param)
                if ret is not None:
                    return ret

        return None

    def foreach_inrow(self, f, out_row, in_row, param=None):
        for i in range(3**2):
            out_col = i // 3
            in_col  = i %  3

            ret = f(out_row, out_col, in_row, in_col, param)
            if ret is not None:
                return ret

        return None

    def foreach_incol(self, f, out_col, in_col, param=None):
        for i in range(3**2):
            out_row = i // 3
            in_row  = i %  3

            ret = f(out_row, out_col, in_row, in_col, param)
            if ret is not None:
                return ret

        return None

    def foreach_row(self, f, param=None):
        for out_row in range(3):
            for in_row in range(3):
                ret = f(out_row, in_row, param)
                if ret is not None:
                    return ret

        return None

    def foreach_column(self, f, param=None):
        for out_col in range(3):
            for in_col in range(3):
                ret = f(out_col, in_col, param)
                if ret is not None:
                    return ret
        
        return None

    def on_base_out_remove(self, row, col, param):
        for num in [x+1 for x in range(9)]:
            if not self.contains_inner_box(row, col, num):
                continue

            self.erase_in_box(row, col, num)

    def on_base_row_remove(self, out_row, in_row, param):
        for num in [x+1 for x in range(9)]:
            if not self.contains_row(out_row, in_row, num):
                continue

            self.erase_in_row(out_row, in_row, num)

    def on_base_column_remove(self, out_col, in_col, param):
        for num in [x+1 for x in range(9)]:
            if not self.contains_column(out_col, in_col, num):
                continue

            self.erase_in_column(out_col, in_col, num)

    def on_find(self, out_row, out_col, in_row, in_col, param):
        cell = self[out_row, out_col, in_row, in_col]
        value = cell.get_candidate()
        if value is None:
            return None

        self[out_row, out_col, in_row, in_col]._num = value
        self[out_row, out_col, in_row, in_col]._candidates = []
        return True, out_row, out_col, in_row, in_col, value

    def on_finish(self, out_row, out_col, in_row, in_col, param):
        cell = self[out_row, out_col, in_row, in_col]
        if cell.has_candidates():
            return False

        return None

    def is_finish(self):
        if self.foreach_all(self.on_finish) is False:
            return False

        return True

    def is_valid(self):
        valid_last_row = False
        for col in range(3**2):
            if self[2, col // 3, 2, col % 3].get_value() is not None:
                valid_last_row = True
                break

        valid_last_col = False
        for row in range(3**2):
            if self[row // 3, 2, row % 3, 2].get_value() is not None:
                valid_last_col = True
                break

        return valid_last_row and valid_last_col



    # Check table is valid after set number of cell
    # Return value
    #   - success : return True if 'num' is correct value for position 'coordination'
    def predict(self, coordination, num):
        out_row, out_col, in_row, in_col = coordination
        cell = self[out_row, out_col, in_row, in_col]
        if not cell.has_candidates():
            return False

        new_table = self.clone(self._root)
        new_table[out_row, out_col, in_row, in_col]._num = num
        new_table[out_row, out_col, in_row, in_col]._candidates = []
        return new_table.continuable()

    # Get value that table is valid
    # Return value
    #   - success : return True if table is valid else False
    def continuable(self):
        try:
            while True:
                success, out_row, out_col, in_row, in_col, num = self.next()
                if not success:
                    break

            return self.is_finish()
        except Exception as e:
            return False


    # Get next cell number
    # Return value
    #   - success   : return True if cell has valid value else False
    #   - out_row   : row index of outer box
    #   - out_col   : column index of outer box
    #   - in_row    : row index of inner box
    #   - in_col    : column index of inner box
    #   - candidate : number of cell
    def next(self):
        try:
            if self.is_finish():
                raise Exception()

            # Remove candidates using common algorithm
            self.foreach_out(self.on_base_out_remove)       # Remove out candidates
            self.foreach_row(self.on_base_row_remove)       # Remove row candidates
            self.foreach_column(self.on_base_column_remove) # Remove column candidates
            ret = self.foreach_all(self.on_find)            # Find next number
            if ret is not None:
                return ret

            # Remove exposure circulation numbers
            self.foreach_out(self.on_remove_exposed_cnum_out)
            self.foreach_row(self.on_remove_exposed_cnum_row)
            self.foreach_column(self.on_remove_exposed_cnum_col)
            ret = self.foreach_all(self.on_find)
            if ret is not None:
                return ret

            # Remove hidden circulation numbers
            self.foreach_out(self.on_remove_hidden_cnum_out)
            self.foreach_row(self.on_remove_hidden_cnum_row)
            self.foreach_column(self.on_remove_hidden_cnum_col)
            ret = self.foreach_all(self.on_find)
            if ret is not None:
                return ret

            # Remove candidates using x-wing algorithm
            for i in range(3**2):
                self.remove_xwing_row(i)
            for i in range(3**2):
                self.remove_xwing_col(i)
            if ret is not None:
                return ret

            # Get cell has least candidates
            coordination = self.get_least_candidate_coordination()
            if coordination is None:
                raise Exception()

            # Predict from candidate
            cell = self[coordination]
            for candidate in cell._candidates:
                out_row, out_col, in_row, in_col = coordination
                if not self.predict(coordination, candidate):
                    continue

                self[coordination]._candidates = []
                self[coordination]._num = candidate
                return True, out_row, out_col, in_row, in_col, candidate

            raise Exception()

        except Exception as e:
            return False, None, None, None, None, None

    def exclude_out(self, out_row, out_col, cells, f=None):
        result = []
        for i in range(3**2):
            found = False
            row = i // 3
            col = i %  3

            for cell in cells:
                if self[out_row, out_col, row, col] is cell:
                    found = True
                    break

            if not found:
                if f is not None and not f(self[out_row, out_col, row, col]):
                    continue

                result.append(self[out_row, out_col, row, col])

        return result

    def get_cnumbers_subsets_out(self, out_row, out_col, cnumbers):
        cells = []
        for in_row in range(3):
            for in_col in range(3):
                cell = self[out_row, out_col, in_row, in_col]
                if cell.has_candidates() and set(cell._candidates).issubset(cnumbers):
                    cells.append(cell)

        return cells

    def on_remove_exposed_cnum_out(self, out_row, out_col, param):
        for i in range(3**2):
            in_row = i // 3
            in_col = i %  3
            
            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            self.foreach_inbox(self.on_remove_exposed_cnum_out_partition, out_row, out_col, cell)

    def on_remove_exposed_cnum_out_partition(self, out_row, out_col, in_row, in_col, cell):
        if not self[out_row, out_col, in_row, in_col].has_candidates():
            return None

        cnumbers = set(cell._candidates + self[out_row, out_col, in_row, in_col]._candidates)
        subset_cells = self.get_cnumbers_subsets_out(out_row, out_col, cnumbers)
        if len(subset_cells) != len(cnumbers):
            return None

        excluded_cells = self.exclude_out(out_row, out_col, subset_cells, lambda cell: cell.has_candidates())
        if len(excluded_cells) == 0:
            return None

        for excluded_cell in excluded_cells:
            excluded_cell.erase_candidate(cnumbers)

        return None

    def exclude_row(self, out_row, in_row, cells, f=None):
        result = []
        for i in range(3**2):
            found = False
            out_col = i // 3
            in_col  = i %  3

            for cell in cells:
                if self[out_row, out_col, in_row, in_col] is cell:
                    found = True
                    break

            if not found:
                if f is not None and not f(self[out_row, out_col, in_row, in_col]):
                    continue

                result.append(self[out_row, out_col, in_row, in_col])

        return result

    def get_cnumbers_subsets_row(self, out_row, in_row, cnumbers):
        cells = []
        for out_col in range(3):
            for in_col in range(3):
                cell = self[out_row, out_col, in_row, in_col]
                if cell.has_candidates() and set(cell._candidates).issubset(cnumbers):
                    cells.append(cell)

        return cells

    def on_remove_exposed_cnum_row(self, out_row, in_row, param):
        for i in range(3**2):
            out_col = i // 3
            in_col  = i %  3
            
            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            self.foreach_inrow(self.on_remove_exposed_cnum_row_partition, out_row, in_row, cell)

    def on_remove_exposed_cnum_row_partition(self, out_row, out_col, in_row, in_col, cell):
        if not self[out_row, out_col, in_row, in_col].has_candidates():
            return None

        cnumbers = set(cell._candidates + self[out_row, out_col, in_row, in_col]._candidates)
        subset_cells = self.get_cnumbers_subsets_row(out_row, in_row, cnumbers)
        if len(subset_cells) != len(cnumbers):
            return None

        excluded_cells = self.exclude_row(out_row, in_row, subset_cells, lambda cell: cell.has_candidates())
        if len(excluded_cells) == 0:
            return None

        for excluded_cell in excluded_cells:
            excluded_cell.erase_candidate(cnumbers)

        return None

    def exclude_col(self, out_col, in_col, cells, f=None):
        result = []
        for i in range(3**2):
            found = False
            out_row = i // 3
            in_row  = i %  3

            for cell in cells:
                if self[out_row, out_col, in_row, in_col] is cell:
                    found = True
                    break

            if not found:
                if f is not None and not f(self[out_row, out_col, in_row, in_col]):
                    continue

                result.append(self[out_row, out_col, in_row, in_col])

        return result

    def get_cnumbers_subsets_col(self, out_col, in_col, cnumbers):
        cells = []
        for out_row in range(3):
            for in_row in range(3):
                cell = self[out_row, out_col, in_row, in_col]
                if cell.has_candidates() and set(cell._candidates).issubset(cnumbers):
                    cells.append(cell)

        return cells

    def on_remove_exposed_cnum_col(self, out_col, in_col, param):
        for i in range(3**2):
            out_row = i // 3
            in_row  = i %  3
            
            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            self.foreach_incol(self.on_remove_exposed_cnum_col_partition, out_col, in_col, cell)

    def on_remove_exposed_cnum_col_partition(self, out_row, out_col, in_row, in_col, cell):
        if not self[out_row, out_col, in_row, in_col].has_candidates():
            return None

        cnumbers = set(cell._candidates + self[out_row, out_col, in_row, in_col]._candidates)
        subset_cells = self.get_cnumbers_subsets_col(out_col, in_col, cnumbers)
        if len(subset_cells) != len(cnumbers):
            return None

        excluded_cells = self.exclude_col(out_col, in_col, subset_cells, lambda cell: cell.has_candidates())
        if len(excluded_cells) == 0:
            return None

        for excluded_cell in excluded_cells:
            excluded_cell.erase_candidate(cnumbers)

        return None

    def on_remove_hidden_cnum_out(self, out_row, out_col, param):
        amount = []
        for i in range(3**2):
            in_row = i // 3
            in_col = i %  3
            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            amount += cell._candidates

        all_candidates = set(amount)
        combination_list = []
        for i in range(2, len(all_candidates)):
            combination_list += list(combinations(all_candidates, i))

        for combination in combination_list:
            self.remove_hidden_cnum_out(out_row, out_col, combination)

    def remove_hidden_cnum_out(self, out_row, out_col, candidates):
        cells = []
        for in_row in range(3):
            for in_col in range(3):
                cell = self[out_row, out_col, in_row, in_col]
                if not cell.has_candidates():
                    continue

                if not any(e in candidates for e in cell._candidates):
                    continue

                cells.append(cell)

        if len(cells) != len(candidates):
            return None

        for cell in cells:
            cell.remain_candidates(candidates)

    def on_remove_hidden_cnum_row(self, out_row, in_row, param):
        amount = []
        for i in range(3**2):
            out_col = i // 3
            in_col  = i %  3
            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            amount += cell._candidates

        all_candidates = set(amount)
        combination_list = []
        for i in range(2, len(all_candidates)):
            combination_list += list(combinations(all_candidates, i))

        for combination in combination_list:
            self.remove_hidden_cnum_row(out_row, in_row, combination)

    def remove_hidden_cnum_row(self, out_row, in_row, candidates):
        cells = []
        for out_col in range(3):
            for in_col in range(3):
                cell = self[out_row, out_col, in_row, in_col]
                if not cell.has_candidates():
                    continue

                if not any(e in candidates for e in cell._candidates):
                    continue

                cells.append(cell)

        if len(cells) != len(candidates):
            return None

        for cell in cells:
            cell.remain_candidates(candidates)

    def on_remove_hidden_cnum_col(self, out_col, in_col, param):
        amount = []
        for i in range(3**2):
            out_row = i // 3
            in_row  = i %  3
            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            amount += cell._candidates

        all_candidates = set(amount)
        combination_list = []
        for i in range(2, len(all_candidates)):
            combination_list += list(combinations(all_candidates, i))

        for combination in combination_list:
            self.remove_hidden_cnum_col(out_col, in_col, combination)

    def remove_hidden_cnum_col(self, out_col, in_col, candidates):
        cells = []
        for out_row in range(3):
            for in_row in range(3):
                cell = self[out_row, out_col, in_row, in_col]
                if not cell.has_candidates():
                    continue

                if not any(e in candidates for e in cell._candidates):
                    continue

                cells.append(cell)

        if len(cells) != len(candidates):
            return None

        for cell in cells:
            cell.remain_candidates(candidates)

    def get_contains_candidate_coordinations_row(self, out_row, in_row, num):
        coordinations = []
        for col in range(3**2):
            out_col = col // 3
            in_col  = col %  3
            cell = self[out_row, out_col, in_row, in_col]
            if cell.has_candidates():
                continue

            if not cell.contains_candidate(num):
                continue

            coordinations.append((out_col, in_col))

        return coordinations


    def remove_xwing_row(self, num):
        for row in range(3**2 - 1):
            out_row = row // 3
            in_row  = row %  3

            coordinations_1st = self.get_contains_candidate_coordinations_row(out_row, in_row, num)
            if len(coordinations_1st) != 2:
                continue

            cell_1st = self[out_row, coordinations_1st[0][0], in_row, coordinations_1st[0][1]]
            cell_2nd = self[out_row, coordinations_1st[1][0], in_row, coordinations_2nd[1][1]]

            for next_row in range(row+1, 3**2):
                next_out_row = next_row // 3
                next_in_row  = next_row %  3

                coordinations_2nd = self.get_contains_candidate_coordinations_row(next_out_row, next_in_row, num)
                if len(coordinations_2nd) != 2:
                    continue

                if coordinations_1st != coordinations_2nd:
                    continue

                cell_3th = self[next_out_row, coordinations_2nd[0][0], next_in_row, coordinations_2nd[0][1]]
                cell_4th = self[next_out_row, coordinations_2nd[1][0], next_in_row, coordinations_2nd[1][1]]

                self.erase_in_column(coordinations_1st[0][0], coordinations_1st[0][1], num, lambda cell, c_out_row, c_out_col, c_in_row, c_in_col: not (cell is cell_1st or cell is cell_2nd or cell is cell_3th or cell is cell_4th))
                self.erase_in_column(coordinations_1st[1][0], coordinations_1st[1][1], num, lambda cell, c_out_row, c_out_col, c_in_row, c_in_col: not (cell is cell_1st or cell is cell_2nd or cell is cell_3th or cell is cell_4th))
                return num

        return None

    def get_contains_candidate_coordinations_col(self, out_col, in_col, num):
        coordinations = []
        for row in range(3**2):
            out_row = row // 3
            in_row  = row %  3
            cell = self[out_row, out_col, in_row, in_col]
            if cell.has_candidates():
                continue

            if not cell.contains_candidate(num):
                continue

            coordinations.append((out_row, in_row))

        return coordinations


    def remove_xwing_col(self, num):
        for col in range(3**2 - 1):
            out_col = col // 3
            in_col  = col %  3

            coordinations_1st = self.get_contains_candidate_coordinations_col(out_col, in_col, num)
            if len(coordinations_1st) != 2:
                continue

            cell_1st = self[coordinations_1st[0][0], out_col, coordinations_1st[0][1], in_col]
            cell_2nd = self[coordinations_1st[1][0], out_col, coordinations_2nd[1][1], in_col]

            for next_col in range(col+1, 3**2):
                next_out_col = next_col // 3
                next_in_col  = next_col %  3

                coordinations_2nd = self.get_contains_candidate_coordinations_col(next_out_col, next_in_col, num)
                if len(coordinations_2nd) != 2:
                    continue

                if coordinations_1st != coordinations_2nd:
                    continue

                cell_3th = self[coordinations_2nd[0][0], next_out_col, coordinations_2nd[0][1], next_in_col]
                cell_4th = self[coordinations_2nd[1][0], next_out_col, coordinations_2nd[1][1], next_in_col]

                self.erase_in_row(coordinations_1st[0][0], coordinations_1st[0][1], num, lambda cell, c_out_row, c_out_col, c_in_row, c_in_col: not (cell is cell_1st or cell is cell_2nd or cell is cell_3th or cell is cell_4th))
                self.erase_in_row(coordinations_1st[1][0], coordinations_1st[1][1], num, lambda cell, c_out_row, c_out_col, c_in_row, c_in_col: not (cell is cell_1st or cell is cell_2nd or cell is cell_3th or cell is cell_4th))
                return num

        return None

    def get_least_candidate_coordination(self):
        count = 9
        coordination = None
        for i in range(3**4):
            out_row = (i // 27)
            out_col = (i // 9) % 3
            in_row  = (i // 3) % 3
            in_col  = (i % 3)

            cell = self[out_row, out_col, in_row, in_col]
            if not cell.has_candidates():
                continue

            if len(cell._candidates) < count:
                count = len(cell._candidates)
                coordination = (out_row, out_col, in_row, in_col)

        return coordination

    def to_list(self):
        root = []
        for out_row in range(3):
            out_row_list = []
            for out_col in range(3):
                out_col_list = []
                for in_row  in range(3):
                    in_row_list = []
                    for in_col in range(3):
                        cell = self[out_row, out_col, in_row, in_col]
                        if not cell.has_candidates():
                            in_row_list.append(cell._candidates)
                        else:
                            in_row_list.append(cell._num)

                    out_col_list.append(in_row_list)
                out_row_list.append(out_col_list)
            root.append(out_row_list)

        return root