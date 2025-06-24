import sys
print('Python path:', sys.path)
try: 
    import pyvips; 
    print('pyvips: OK')
except Exception as e: 
    print('pyvips error:', e)
 
try: 
    import numpy; 
    print('numpy: OK')
except Exception as e: 
    print('numpy error:', e)