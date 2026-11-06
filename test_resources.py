import urllib.request
import re

def test_resources():
    base_url = 'https://manhcuong-code.github.io/KimLongEcoStay/login/'
    req = urllib.request.Request(base_url, headers={'User-Agent': 'Mozilla/5.0'})
    html = urllib.request.urlopen(req).read().decode('utf-8')

    pattern = r'(?:href|src)=["\']([^"\']+)["\']'
    links = re.findall(pattern, html)
    print(f'Total resources referenced in login page: {len(links)}')

    results = []
    for r in sorted(set(links)):
        if r.startswith('http://') or r.startswith('https://'):
            target = r
        elif r.startswith('../'):
            target = 'https://manhcuong-code.github.io/KimLongEcoStay/' + r[3:]
        elif r.startswith('/'):
            target = 'https://manhcuong-code.github.io' + r
        else:
            target = 'https://manhcuong-code.github.io/KimLongEcoStay/login/' + r

        try:
            req_res = urllib.request.Request(target, headers={'User-Agent': 'Mozilla/5.0'})
            res_res = urllib.request.urlopen(req_res)
            print(f'[200 OK] {r} -> ({res_res.status})')
            results.append((r, True, res_res.status))
        except urllib.error.HTTPError as e:
            print(f'[FAIL {e.code}] {r} -> {target}')
            results.append((r, False, e.code))
        except Exception as e:
            print(f'[ERR] {r} -> {e}')
            results.append((r, False, str(e)))

    failures = [res for res in results if not res[1]]
    if failures:
        print(f'\nFound {len(failures)} broken resource links!')
    else:
        print('\nALL resources referenced loaded with 200 OK!')

if __name__ == '__main__':
    test_resources()
