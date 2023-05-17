import math
import pickle
import os
import random

SUCCESS             = 0  # 성공 인덱스
FAILED              = 1  # 실패 인덱스
FIRST_LINE          = 0  # 첫번째 라인 인덱스
SECOND_LINE         = 1  # 두번째 라인 인덱스
THIRD_LINE          = 2  # 세번째 라인 인덱스
MAX_LINE            = 3
MAX_PROB            = 0.75
MIN_PROB            = 0.25
DIFF_PROB           = 0.10

def next_prob(current_prob, success):
    if success:
        return max(MIN_PROB, current_prob - DIFF_PROB)

    else:
        return min(MAX_PROB, current_prob + DIFF_PROB)

def next_state(current_state, success):
    if success:
        return (current_state[SUCCESS]+1, current_state[FAILED])
    else:
        return (current_state[SUCCESS], current_state[FAILED]+1)

def expect_value(stone_conf, state_line):
    return stone_conf['chance'] - state_line[FAILED]

def is_base_success(stone_conf, state, min_success):
    no_debuff = (expect_value(stone_conf, state[THIRD_LINE]) <= 4)
    activated = (state[FIRST_LINE][SUCCESS] + state[SECOND_LINE][SUCCESS] >= min_success)

    return no_debuff and activated

def is_success_97(stone_conf, state):
    if not is_base_success(stone_conf, state, 16):
        return False

    validated = not (state[FIRST_LINE][SUCCESS] <= 8 and state[SECOND_LINE][SUCCESS] <= 8)
    if not validated:
        return False

    return True

def is_success_77(stone_conf, state):
    if not is_base_success(stone_conf, state, 14):
        return False

    validated = not ((state[FIRST_LINE][SUCCESS] <= 6 and state[SECOND_LINE][SUCCESS] <= 8) or (state[SECOND_LINE][SUCCESS] <= 6 and state[FIRST_LINE][SUCCESS] <= 8))
    if not validated:
        return False

    return True

def is_success_97_1st(stone_conf, state):
    if not is_base_success(stone_conf, state, 16):
        return False

    case1 = (state[FIRST_LINE][SUCCESS] >= 9 and state[SECOND_LINE][SUCCESS] >= 7)
    case2 = (state[FIRST_LINE][SUCCESS] >= 6 and state[SECOND_LINE][SUCCESS] >= 10)

    return case1 or case2

def is_success_97_2nd(stone_conf, state):
    if not is_base_success(stone_conf, state, 16):
        return False

    case1 = (state[FIRST_LINE][SUCCESS] >= 7 and state[SECOND_LINE][SUCCESS] >= 9)
    case2 = (state[FIRST_LINE][SUCCESS] >= 10 and state[SECOND_LINE][SUCCESS] >= 6)

    return case1 or case2

def is_base_failed(stone_conf, state, amount_success):
    active_debuff = (state[THIRD_LINE][SUCCESS] >= 5)
    if active_debuff:
        return True
    
    best_success = expect_value(stone_conf, state[FIRST_LINE]) + expect_value(stone_conf, state[SECOND_LINE])
    if best_success < amount_success:
        return True
    
    return False

def is_failed_97(stone_conf, state):
    if is_base_failed(stone_conf, state, 16):
        return True

    if expect_value(stone_conf, state[FIRST_LINE]) <= 8 and expect_value(stone_conf, state[SECOND_LINE]) <= 8:
        return True

    return False

def is_failed_77(stone_conf, state):
    if is_base_failed(stone_conf, state, 14):
        return True

    if expect_value(stone_conf, state[FIRST_LINE]) <= 6 and expect_value(stone_conf, state[SECOND_LINE]) <= 8:
        return True
    
    if expect_value(stone_conf, state[FIRST_LINE]) <= 8 and expect_value(stone_conf, state[SECOND_LINE]) <= 6:
        return True
    
    return False

def is_failed_97_1st(stone_conf, state):
    if is_base_failed(stone_conf, state, 16):
        return True

    case1 = (expect_value(stone_conf, state[FIRST_LINE]) < 9 or expect_value(stone_conf, state[SECOND_LINE]) < 7)
    case2 = (expect_value(stone_conf, state[FIRST_LINE]) < 6 or expect_value(stone_conf, state[SECOND_LINE]) < 10)
    return case1 and case2

def is_failed_97_2nd(stone_conf, state):
    if is_base_failed(stone_conf, state, 16):
        return True

    case1 = (expect_value(stone_conf, state[FIRST_LINE]) < 7 or expect_value(stone_conf, state[SECOND_LINE]) < 9)
    case2 = (expect_value(stone_conf, state[FIRST_LINE]) < 10 or expect_value(stone_conf, state[SECOND_LINE]) < 6)
    return case1 and case2

def is_success_99(stone_conf, state):
    if not is_base_success(stone_conf, state, 18):
        return False

    return True


def is_failed_99(stone_conf, state):
    if is_base_failed(stone_conf, state, 18):
        return True

    if expect_value(stone_conf, state[FIRST_LINE]) <= 10 and expect_value(stone_conf, state[SECOND_LINE]) <= 8:
        return True

    if expect_value(stone_conf, state[FIRST_LINE]) <= 8 and expect_value(stone_conf, state[SECOND_LINE]) <= 10:
        return True

    return False

callback_conf = {
    '97': {
        'success': is_success_97,
        'failed': is_failed_97
    },
    '97_1st': {
        'success': is_success_97_1st,
        'failed': is_failed_97_1st
    },
    '97_2nd': {
        'success': is_success_97_2nd,
        'failed': is_failed_97_2nd
    },
    '77': {
        'success': is_success_77,
        'failed': is_failed_77
    },
    '99': {
        'success': is_success_99,
        'failed': is_failed_99
    }
}
rating_conf = {
    'rare': {'chance': 6 },
    'epic': {'chance': 8 },
    'legendary': {'chance': 9, 'pheon': 5 },
    'relic': {'chance': 10, 'pheon': 9 }
}



class simulator:
    def __init__(self, target, rating, cache=None):
        self.callback_conf = callback_conf[target]
        self.stone_conf = rating_conf[rating]
        self.file_name = f"cache_{target}_{rating}"

        if cache is None:
            if os.path.exists(self.file_name):
                with open(self.file_name, 'rb') as f:
                    self.cache = pickle.load(f)
        else:
            self.cache = cache

    def is_success(self, state):
        return self.callback_conf['success'](self.stone_conf, state)

    def is_failed(self, state):
        return self.callback_conf['failed'](self.stone_conf, state)
    
    def result(self, state):
        if self.is_success(state):
            return True

        if self.is_failed(state):
            return False

        return None

    def fn(self, prob, selection, state):
        """ 주어진 상황에서 선택했을 때 최종적으로 목표에 달성할 수 있는 확률을 구함
        :param prob: 현재 확률 (1.0 = 100%)
        :param selection: 선택지 (FIRST, SECOND, THIRD)
        :param state: [(첫째줄성공횟수, 첫째줄실패횟수), (둘째줄성공횟수, 둘째줄실패횟수), (셋째줄성공횟수, 셋째줄실패횟수)]
        :return: 주어진 상황에서의 최종목표 달성 확률
        """
        is_debuff = (selection == THIRD_LINE)
        step = sum(success+failed for success, failed in state)
        key = f"{step}_{round(prob*100)}_{selection}/{'_'.join(f'{success}_{failed}' for success, failed in state)}"
        if key in self.cache:
            return self.cache[key]

        if self.callback_conf['success'](self.stone_conf, state):
            self.cache[key] = 1
            return self.cache[key]

        if self.callback_conf['failed'](self.stone_conf, state):
            self.cache[key] = 0
            return self.cache[key]
        
        if (sum(state[selection]) >= self.stone_conf['chance']):
            self.cache[key] = 0
            return self.cache[key]
        
        next_states = [
            [next_state(x, True) if i == selection else x for i, x in enumerate(state)],
            [next_state(x, False) if i == selection else x for i, x in enumerate(state)]
        ]
        
        heuristic_prob = prob if not is_debuff else 1.0 - prob
        ns = (next_states[FAILED] if is_debuff else next_states[SUCCESS], next_states[FAILED] if not is_debuff else next_states[SUCCESS])

        basic_prob = heuristic_prob * max(self.fn(next_prob(prob, not is_debuff), i, ns[SUCCESS]) for i in range(MAX_LINE))
        additional_prob = (1.0 - heuristic_prob) * max(self.fn(next_prob(prob, is_debuff), i, ns[FAILED]) for i in range(MAX_LINE))

        self.cache[key] = basic_prob + additional_prob
        return self.cache[key]

    def simulate(self):
        """ 어빌리티스톤을 세공함
        :param callback: 한번 눌렀을 때 호출될 콜백함수
        :return: (목표달성여부, 최종결과)
        """
        current_prob = MAX_PROB
        state = [(0, 0), (0, 0), (0, 0)]

        while True:
            probs = []
            for selection in range(MAX_LINE):
                probs.append(self.fn(current_prob, selection, state))

            final_prob = max(*probs)
            selection = random.choice([i for i, x in enumerate(probs) if x == final_prob])
            success = random.randrange(100) < math.ceil(current_prob * 100)
            np = next_prob(current_prob, success)
            state[selection] = next_state(state[selection], success)

            history = {
                'current prob': current_prob,
                'selection': selection,
                'final prob': final_prob,
                'result': [x for x in state]
            }

            result = self.result(state)
            yield result, history
            if result is not None:
                break
            
            current_prob = np