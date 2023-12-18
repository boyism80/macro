import loa
import random

def engraving(app, name):
    found = app.Detect(name, area={"x": 618, "y": 337, "width": 102, "height": 87}, timeout=0)
    if name in found:
        return 1

    found = app.Detect(name, area={"x": 621, "y": 428, "width": 99, "height": 92}, timeout=0)
    if name in found:
        return 2

    return 0
    

def state(app):
    line1 = {"x": 723, "y": 367, "width": 397, "height": 46}
    line2 = {"x": 721, "y": 464, "width": 399, "height": 31}
    line3 = {"x": 716, "y": 591, "width": 404, "height": 36}

    found1 = len(app.DetectAll('stone-success', area=line1))
    found2 = len(app.DetectAll('stone-failed', area=line1))
    found3 = len(app.DetectAll('stone-success', area=line2))
    found4 = len(app.DetectAll('stone-failed', area=line2))
    found5 = len(app.DetectAll('stone-debuff-success', area=line3))
    found6 = len(app.DetectAll('stone-failed', area=line3))

    return [
        (found1, found2),
        (found3, found4),
        (found5, found6)
    ]


def percent(app):
    percents = ('25%', '35%', '45%', '55%', '65%', '75%')
    found = app.Detect(percents, 0.9, area={'x': 1100, 'y': 300, 'width': 300, 'height': 100})

    max_match = 0.0
    matched = None
    for p in percents:
        if p not in found:
            continue

        if max_match >= found[p]['percent']:
            continue

        max_match = found[p]['percent']
        matched = p

    if not matched:
        return None

    percent_maps = {'25%': 0.25, '35%': 0.35, '45%': 0.45, '55%': 0.55, '65%': 0.65, '75%': 0.75}
    return percent_maps[matched]

def facet_stone(app, engraving_name, count):
    app.Detect('stone-main')

    button_position = {
        '0': (1170, 380),
        '1': (1170, 480),
        '2': (1170, 600)
    }

    stone_index = 0
    for stone_index in range(count):
        stone_position = (300, 150 + stone_index * 56)
        app.target.Click(stone_position)
        app.target.Click((825, 90))
        app.Sleep(200)

        while True:
            app.clear_log()
            current_state = state(app)

            engraving_line = engraving(app, engraving_name) if engraving_name else None
            if engraving_line == 1:
                ist = app.heap['97_2nd']
            elif engraving_line == 2:
                ist = app.heap['97_1st']
            else:
                ist = app.heap['97']

            success = ist.result(current_state)
            if success == False:
                break

            if success == True:
                return True

            current_percent = percent(app)

            probs = []
            for selection in range(loa.MAX_LINE):
                probs.append(ist.fn(current_percent, selection, current_state) * 100.0)

            for selection, prob in enumerate(probs):
                app.add_log(f"{selection + 1} >> {prob:.3f}%")

            final_prob = max(*probs)
            selection = random.choice([i for i, x in enumerate(probs) if x == final_prob])

            app.add_log(f"next selection : {selection + 1} ({final_prob:.3f}%)")

            position = button_position[f'{selection}']
            app.target.Click(position)
            app.Sleep(500)

    app.target.Escape()
    return False

def enter_pw(app, pw):
    sprite_names = []
    for c in str(pw):
        sprite_name = 'num-' + c
        sprite_names.append(sprite_name)
    sprite_names = tuple(set(sprite_names))

    found = app.Detect(sprite_names, 0.8)
    for c in str(pw):
        sprite_name = 'num-' + c
        app.target.Click(found[sprite_name]['position'])

def buy_pheon(app):
    app.target.KeyPress(115) # F4
    found = app.Detect('convenience')
    app.target.Click(found['convenience']['position'])

    found = app.Detect('pheon')
    app.target.Click(found['pheon']['position'])

    found = app.Detect('buy')
    app.target.Click(found['buy']['position'])

    found = app.Detect('buy-step-1')
    app.target.Click(found['buy-step-1']['position'])

    found = app.Detect('buy-step-2')
    app.target.Click(found['buy-step-2']['position'])

    enter_pw(app, 557575)

    app.target.Click((985, 680))
    found = app.Detect('confirm')
    app.target.Click(found['confirm']['position'])

    found = app.Detect('accept-reward')
    app.target.Click(found['accept-reward']['position'])

    found = app.Detect('accept-reward-confirm')
    app.target.Click(found['accept-reward-confirm']['position'])

    found = app.Detect('confirm')
    app.target.Click(found['confirm']['position'])

    app.target.Escape()
    app.target.Escape()
    app.target.KeyPress('I')

    found = app.Detect('pheon-icon')
    app.target.RClick(found['pheon-icon']['position'], ('ALT',))
    app.target.Enter()
    app.target.Escape()

def buy_stone(app, slot, count):
    def catch_exception(app, name):
        found = app.Detect((name, 'sold-out', 'buy-failed'), 0.8)
        if 'sold-out' in found:
            app.target.Click((1510, 240))
            app.Sleep(1000)
            return None

        if 'buy-failed' in found:
            found = app.Detect('confirm')
            app.target.Click(found['confirm']['position'])
            app.target.Click((1510, 240))
            app.Sleep(1000)
            return None

        return found[name]['position']

    buy_count = 0

    app.Detect('loa-talk')
    app.target.KeyPress(('ALT', 'Y'))
    found = app.Detect('auction')
    app.target.Click(found['auction']['position'])
    app.target.Click((1575, 240))

    slot_y = 280 + 46*(slot-1)
    app.target.Click((1350, slot_y))
    app.target.Click((900, 570))
    app.target.Click((1000, 910))

    app.Sleep(1000)
    while buy_count < count:
        app.target.Click((575, 333))
        app.target.Click((1565, 925))

        position = catch_exception(app, 'buy-directly')
        if not position:
            continue
        
        app.target.Click(position)

        position = catch_exception(app, 'confirm')
        if not position:
            continue

        app.target.Click(position)

        position = catch_exception(app, 'buy-success')
        if not position:
            continue

        app.target.Escape()
        buy_count = buy_count + 1

    app.target.Escape()
    app.Sleep(2000)

    app.target.KeyPress(('ALT', '`'))
    found = app.Detect('post')
    app.target.Click(found['post']['position'])

    app.Detect('post-main')
    app.target.Click((404, 316))
    app.target.Click((615, 835))
    
    for i in range(2):
        app.target.Escape()

def disassemble_stones(app):
    app.Detect('loa-talk')
    app.target.KeyPress('I')

    found = app.Detect('disassemble')
    app.target.Click(found['disassemble']['position'])

    found = app.Detect('disassemble-relic')
    app.target.Click(found['disassemble-relic']['position'])

    found = app.Detect('disassemble-start')
    app.target.Click(found['disassemble-start']['position'])

    found = app.Detect('confirm')
    app.target.Click(found['confirm']['position'])

    for i in range(2):
        app.target.Escape()