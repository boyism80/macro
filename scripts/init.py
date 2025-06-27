import loa

def callback(app):
    app.ClearLines()

    resources = ('97', '97_1st', '97_2nd')
    for name in resources:
        if name not in app.heap:
            cache = app.LoadCache(f'cache_{name}.dat')
            app.heap[name] = loa.simulator(name, 'relic', cache)
            yield f'{name} cache loaded'

        else:
            yield f'{name} already loaded'

    yield 'init complete'