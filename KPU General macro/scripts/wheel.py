import stone

def receive_epona(app):
    app.target.KeyPress(('ALT', 'J'))

    found = app.Detect('epona-main')

    app.target.Click((425, 240))
    app.Sleep(500)
    app.target.Click((425, 350))

    app.target.Click((1285, 355))
    app.Sleep(500)
    app.target.Click((1285, 430))
    app.Sleep(500)
    app.target.Click((1285, 505))
    app.Sleep(500)
    app.target.Escape()

def bifrost_1(app):
    app.target.KeyPress(('ALT', 'W'))
    app.Detect('bifrost-main')
    app.target.Click((1470, 385))
    app.target.Enter()
    app.Sleep(1000)

    app.Detect('loa-talk')

def fishing(app):
    pivot = app.target.GetCursorPosition()
    while True:
        found = app.Detect('gear-crash', untilDetect=False)
        if 'gear-crash' in found:
          app.target.KeyPress(('ALT', '`'))
            
          found = app.Detect('pet-repair-icon')
          app.target.Click(found['pet-repair-icon']['position'])

          found = app.Detect('repair-life-gear')
          app.target.Click(found['repair-life-gear']['position'])

          found = app.Detect('repair-all')
          app.target.Click(found['repair-all']['position'])

          found = app.Detect('buy-step-2')
          app.target.Click(found['buy-step-2']['position'])

          app.Sleep(2000)
          app.target.Escape()
          app.target.Escape()

        prev = app.target.SetCursorPosition(pivot)
        app.target.KeyPress('W')
        app.Sleep(500)
        app.target.SetCursorPosition(prev)

        found = app.Detect('not-enough-life-energy-2', untilDetect=False)
        if 'not-enough-life-energy-2' in found:
          app.target.KeyPress('I')

          potions = ('life-energy-potion(large)', 'life-energy-potion(normal)', 'life-energy-potion(small)')
          found = app.Detect(potions)
          app.add_log('detected potion')
          app.Sleep(500)

          for potion in potions:
            if potion in found:
                app.target.RClick(found[potion]['position'])
                app.add_log(f'click {potion}')
                break
          
          app.target.Escape()
          continue

        found = app.Detect('fish-catch')
        app.clear_log()
        yield (found['fish-catch']['percent'], found['fish-catch']['position'])
        
        app.target.KeyPress('W')
        app.Sleep(7000)

def stone_simulate(app, slot, engraving_name=None, counts=(5, ), with_pheon=True):
    if with_pheon:
        stone.buy_pheon(app)

    for count in counts:
        stone.buy_stone(app, slot, count)
        app.target.KeyPress('G')
        if stone.facet_stone(app, engraving_name, count):
            return
            
        stone.disassemble_stones(app)

def callback(app):
    return fishing(app)
    # for i in range(10):
    #     stone_simulate(app, 1, engraving_name='adrenaline', with_pheon=False)